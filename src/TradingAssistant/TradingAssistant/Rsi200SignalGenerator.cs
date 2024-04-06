using System.Text;
using Binance.Net.Enums;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200SignalGenerator : IObserver<CandleId>
    {
        private readonly ILogger<Rsi200SignalGenerator> _logger;
        private readonly IPublisher _publisher;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly KlineInterval _timeFrame;
        private readonly int _candlestickSize;
        private IDisposable? _unsubscriber;

        public Rsi200SignalGenerator(ILogger<Rsi200SignalGenerator> logger,
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

        public virtual void SubscribeTo(IObservable<CandleId> provider)
        {
            if (provider is not null)
            {
                _unsubscriber = provider.Subscribe(this);
            }
        }

        public virtual void OnCompleted()
        {
            Unsubscribe();
        }

        public virtual void OnError(Exception error)
        {
        }

        public virtual async void OnNext(CandleId lastCandleId)
        {
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
                return;
            }

            var preferredTimeFrameCandles = candlesticks
                .First(candlestick => candlestick.Key.TimeFrame == _timeFrame).Value
                .Snapshot();
            var time = preferredTimeFrameCandles[_candlestickSize - 1].CloseTime
                .AddSeconds(1)
                .ToLocalTime();
            var entryPrice = preferredTimeFrameCandles[_candlestickSize - 1].ClosePrice;
            var orderedCandlesticks = candlesticks.OrderBy(candlestick => candlestick.Key.TimeFrame)
                .Select(candlestick => candlestick.Value)
                .ToArray();
            var signalOrderSide = GetRsiSignal(orderedCandlesticks);

            if (signalOrderSide is not null)
            {
                var positionSide = signalOrderSide.Value.AsPositionSide();

                _logger.LogInformation("{Time:HH:mm}{NewLine1}" +
                    "Binance{NewLine2}" +
                    "{Symbol}{NewLine3}" +
                    "{PositionSide}{NewLine4}" +
                    "@ {Price}{NewLine5}" +
                    "{TimeFrame}{NewLine6}",
                    time,
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

                await _publisher.Publish(new EmaReversionSignal
                {
                    Time = time,
                    TimeFrame = _timeFrame,
                    Direction = positionSide,
                    Side = signalOrderSide.Value,
                    Symbol = candlesticks.First().Key.Symbol,
                    EntryPrice = entryPrice,
                });

                Console.Beep(frequency: 500, duration: 500);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private OrderSide? GetRsiSignal(CircularTimeSeries<CandlestickId, Candle>[] candlesticks)
        {
            var lengths = new[]
            {
                Length.Five,
                Length.Eight,
                Length.Twenty,
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

            var rsis = lengths.Select(length => GetRsi(candles, length)).ToArray();

            LogRsis(candlestick.Key, candles, lengths, rsis);

            return GetRsisReversionSignal(rsis);
        }

        private static OrderSide? GetRsisReversionSignal(double[][] rsis)
        {
            var rsi200PenultimateSignal = rsis[^1] switch
            {
            [.., <= Rsi.OversoldFor200, _] => OrderSide.Buy,
            [.., >= Rsi.OverboughtFor200, _] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            if (rsi200PenultimateSignal is null)
            {
                return null;
            }

            var fasterRsiPenultimateSignals = rsis.Take(3)
                .Select(rsi => rsi switch
                {
                [.., <= Rsi.Oversold, _] => OrderSide.Buy,
                [.., >= Rsi.Overbought, _] => OrderSide.Sell,

                    _ => default(OrderSide?),
                });

            if (fasterRsiPenultimateSignals.Any(signal => signal != rsi200PenultimateSignal))
            {
                return null;
            }

            var penultimate = Index.FromEnd(2);
            var penultimateSignal = (rsis[0], rsis[1], rsis[2]) switch
            {
                var (rsi5, rsi8, rsi20) when rsi5[penultimate] <= rsi8[penultimate]
                    && rsi8[penultimate] <= rsi20[penultimate] => OrderSide.Buy,
                var (rsi5, rsi8, rsi20) when rsi5[penultimate] >= rsi8[penultimate]
                    && rsi8[penultimate] >= rsi20[penultimate] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            if (penultimateSignal != rsi200PenultimateSignal)
            {
                return null;
            }

            var last = Index.FromEnd(1);
            var lastSignal = (rsis[0], rsis[1], rsis[2]) switch
            {
                var (rsi5, _, rsi20) when rsi5[last] > rsi20[last] => OrderSide.Buy,
                var (rsi5, _, rsi20) when rsi5[last] < rsi20[last] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            if (lastSignal != rsi200PenultimateSignal)
            {
                return null;
            }

            return rsi200PenultimateSignal;
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
