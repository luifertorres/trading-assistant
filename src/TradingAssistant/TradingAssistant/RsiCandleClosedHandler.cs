using System.Text;
using Binance.Net.Enums;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class RsiCandleClosedHandler : INotificationHandler<CandleClosedNotification>
    {
        private readonly ILogger<RsiCandleClosedHandler> _logger;
        private readonly IPublisher _publisher;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly KlineInterval _timeFrame;
        private readonly int _candlestickSize;

        public RsiCandleClosedHandler(ILogger<RsiCandleClosedHandler> logger,
            IConfiguration configuration,
            IPublisher publisher,
            FasterKV<CandleId, Candle> cache)
        {
            _logger = logger;
            _publisher = publisher;
            _cache = cache;
            _timeFrame = configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _candlestickSize = configuration.GetValue<int>("Binance:Service:CandlestickSize");
        }

        public Task Handle(CandleClosedNotification notification, CancellationToken cancellationToken)
        {
            var lastCandleId = notification.CandleId;
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());
            var intervals = new[] { _timeFrame };

            var candlesticks = intervals.ToDictionary(interval => new CandlestickId(lastCandleId.Symbol, interval),
                interval => new CircularTimeSeries<CandlestickId, Candle>(new(lastCandleId.Symbol, interval), _candlestickSize));

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                foreach (var interval in intervals)
                {
                    var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(lastCandleId.OpenTime) / (long)interval;
                    var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * (long)interval);
                    var idsLeft = _candlestickSize - 1;

                    Enumerable.Repeat(lastCandleId.OpenTime, _candlestickSize)
                        .Select(openTime => lastCandleTime.AddSeconds(-((int)interval * idsLeft--)))
                        .Select(openTime => new CandleId(lastCandleId.Symbol, interval, openTime))
                        .ToList()
                        .ForEach(candleId =>
                        {
                            var candle = default(Candle);
                            var status = session.Read(ref candleId, ref candle);

                            if (status.Found)
                            {
                                candlesticks[new(lastCandleId.Symbol, candleId.TimeFrame)].Add(candleId.OpenTime, candle);
                            }
                        });
                }
            }

            if (candlesticks.Any(candlestick => candlestick.Value.Snapshot().Count < _candlestickSize))
            {
                return Task.CompletedTask;
            }

            var preferredTimeFrameCandles = candlesticks
                .First(candlestick => candlestick.Key.TimeFrame == _timeFrame).Value
                .Snapshot();
            var time = preferredTimeFrameCandles[_candlestickSize - 1].OpenTime;
            var entryPrice = preferredTimeFrameCandles[_candlestickSize - 1].ClosePrice;
            var orderedCandlesticks = candlesticks.OrderBy(candlestick => candlestick.Key.TimeFrame)
                .Select(candlestick => candlestick.Value)
                .ToArray();
            var signalOrderSide = GetEntrySignal(orderedCandlesticks);

            if (signalOrderSide is not null)
            {
                var positionSide = signalOrderSide.Value.AsPositionSide();

                _logger.LogInformation("{Time:HH:mm}{NewLine1}" +
                    "Binance{NewLine2}" +
                    "{Symbol}{NewLine3}" +
                    "{PositionSide}{NewLine4}" +
                    "@ {Price}{NewLine5}" +
                    "{TimeFrame}{NewLine6}",
                    time.ToLocalTime(),
                    Environment.NewLine,
                    Environment.NewLine,
                    candlesticks.First().Key.Symbol,
                    Environment.NewLine,
                    EnumConverter.GetString(positionSide).ToUpperInvariant(),
                    Environment.NewLine,
                    entryPrice,
                    Environment.NewLine,
                    EnumConverter.GetString(_timeFrame),
                    Environment.NewLine);

                _publisher.Publish(new TradingSignalNotification(candlesticks.First().Key.Symbol,
                        _timeFrame,
                        time,
                        positionSide,
                        signalOrderSide.Value,
                        entryPrice),
                    cancellationToken);
            }

            return Task.CompletedTask;
        }

        private OrderSide? GetEntrySignal(CircularTimeSeries<CandlestickId, Candle>[] candlesticks)
        {
            var smaLengths = new[]
            {
                Length.Five,
                Length.Ten,
                Length.Twenty,
                Length.Fifty,
                Length.OneHundred,
                Length.TwoHundred
            };

            var rsiLengths = new[]
            {
                Length.Fifty,
                Length.OneHundred,
                Length.TwoHundred
            };

            var candlestick = candlesticks.First(candlestick => candlestick.Key.TimeFrame == _timeFrame);
            var candles = candlestick.Snapshot();
            var initialGap = new CandlestickGap([], candles[0].OpenTime);
            var timeFrameSpan = TimeSpan.FromSeconds((double)candlestick.Key.TimeFrame);
            var gap = candles.Aggregate(initialGap, CandlestickGap.UpdateGapFromCandle);
            var hasMissingCandles = gap.Durations.Exists(duration => duration > timeFrameSpan);

            if (hasMissingCandles)
            {
                _logger.LogWarning("{Symbol} has missing {TimeFrame} candles",
                    candlestick.Key.Symbol,
                    EnumConverter.GetString(candlestick.Key.TimeFrame));

                return null;
            }

            var smas = smaLengths.Select(length => GetSma(candles, length)).ToArray();
            var rsis = rsiLengths.Select(length => GetRsi(candles, length)).ToArray();

            LogRsis(candlestick.Key, candles, rsiLengths, rsis);

            return GetReversionSignal(smas, rsis);
        }

        private static OrderSide? GetReversionSignal(double[][] smas, double[][] rsis)
        {
            var fastRsi = rsis[0];
            var slowRsis = rsis[1..];
            var isFastRsiCrossingUp = slowRsis.All(rsi => fastRsi[^2] < rsi[^2])
                && slowRsis.All(rsi => fastRsi[^1] > rsi[^1]);
            var smasOrderedByAscending = smas.OrderBy(sma => sma[^1]);
            var areSmasOrderedByAscending = smasOrderedByAscending.SequenceEqual(smas);
            var smasOrderedByDescending = smas.OrderByDescending(sma => sma[^1]);
            var areSmasOrderedByDescending = smasOrderedByDescending.SequenceEqual(smas);

            if (isFastRsiCrossingUp && (areSmasOrderedByAscending || areSmasOrderedByDescending))
            {
                return OrderSide.Buy;
            }

            var isFastRsiCrossingDown = slowRsis.All(rsi => fastRsi[^2] > rsi[^2])
                && slowRsis.All(rsi => fastRsi[^1] < rsi[^1]);

            if (isFastRsiCrossingDown && (areSmasOrderedByDescending || areSmasOrderedByAscending))
            {
                return OrderSide.Sell;
            }

            return null;
        }

        private void LogRsis(CandlestickId candlestickId, List<Candle> candles, int[] lengths, double[][] rsis)
        {
            if (candlestickId.Symbol is "BTCUSDT")
            {
                var values = rsis.Select((rsi, index) => $"RSI {lengths[index]}: {rsi[^1]:F2}")
                    .Aggregate(new StringBuilder(), (builder, rsi) => builder.Append(rsi).AppendLine());

                _logger.LogDebug("{Symbol} {TimeFrame}{NewLine1}" +
                    "{OpenTime:yyyy-MM-dd HH:mm}{NewLine2}" +
                    "{Rsis}",
                    candlestickId.Symbol, EnumConverter.GetString(candlestickId.TimeFrame), Environment.NewLine,
                    candles.ToArray()[^1].OpenTime.ToLocalTime(), Environment.NewLine,
                    values.ToString());
            }
        }

        private static double[] GetSma(List<Candle> candles, int length, PeriodSize? higherTimeFrame = null)
        {
            var quotes = candles.Select(ToQuote).Validate();

            var postWarmupPeriod = length;

            if (higherTimeFrame.HasValue)
            {
                return quotes.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(length + postWarmupPeriod)
                    .GetSma(length)
                    .Select(result => result.Sma.GetValueOrDefault())
                    .ToArray();
            }

            return quotes.TakeLast(length + postWarmupPeriod)
                .GetSma(length)
                .Select(result => result.Sma.GetValueOrDefault())
                .ToArray();
        }

        private static double[] GetRsi(List<Candle> candles, int length, PeriodSize? higherTimeFrame = null)
        {
            var quotes = candles.Select(ToQuote).Validate();

            var postWarmupPeriod = length * 10;

            if (higherTimeFrame.HasValue)
            {
                return quotes.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(length + postWarmupPeriod)
                    .GetRsi(length)
                    .Select(rsi => rsi.Rsi.GetValueOrDefault())
                    .ToArray();
            }

            return quotes.TakeLast(length + postWarmupPeriod)
                .GetRsi(length)
                .Select(rsi => rsi.Rsi.GetValueOrDefault())
                .ToArray();
        }

        private static Quote ToQuote(Candle candle)
        {
            return new Quote
            {
                Date = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
            };
        }
    }
}
