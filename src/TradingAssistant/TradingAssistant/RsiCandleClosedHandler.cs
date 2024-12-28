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
        public static readonly Index Penultimate = ^2;
        public static readonly Index Last = ^1;

        private readonly ILogger<RsiCandleClosedHandler> _logger;
        private readonly IPublisher _publisher;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly KlineInterval _timeFrame;
        private readonly int _candlestickSize;
        private readonly int _rsiPatternLookbackPeriods;

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
            _rsiPatternLookbackPeriods = Math.Max(60 * 60 * 24 / (int)_timeFrame, 1);
        }

        public async Task Handle(CandleClosedNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1_000, cancellationToken);

            var lastCandleId = notification.CandleId;
            var candlestick = GetCandlestick(lastCandleId);

            if (candlestick.Count == 0)
            {
                return;
            }

            var bitcoin = GetCandlestick(lastCandleId with { Symbol = "BTCUSDT" });

            if (bitcoin.Count == 0)
            {
                return;
            }

            if (!candlestick.IsCorrelatedWith(bitcoin))
            {
                var withoutQuoteAsset = ..^4;

                _logger.LogDebug("{Symbol} is not correlated with {Bitcoin}",
                    lastCandleId.Symbol[withoutQuoteAsset], bitcoin[Last].Symbol[withoutQuoteAsset]);
            }

            var signalOrderSide = GetEntrySignal(candlestick);

            if (signalOrderSide.HasValue)
            {
                var time = candlestick[Last].OpenTime;
                var entryPrice = candlestick[Last].ClosePrice;
                var orderSide = signalOrderSide.Value;
                var positionSide = orderSide.AsPositionSide();

                NotifyToTradingSharksArmy(lastCandleId, time, entryPrice, positionSide);

                _ = _publisher.Publish(new TradingSignalNotification(lastCandleId.Symbol,
                        _timeFrame,
                        time,
                        positionSide,
                        orderSide,
                        entryPrice),
                    cancellationToken);
            }

            return;
        }

        private void NotifyToTradingSharksArmy(CandleId lastCandleId, DateTime time, decimal entryPrice, PositionSide positionSide)
        {
            var signalSymbolBaseAsset = lastCandleId.Symbol[..^4];
            var tradingSharksArmyWatchlist = new List<string>
                {
                    "BTC",
                    "ETH",
                    "BNB",
                    "1000LUNC",
                    "DOGE",
                };

            if (tradingSharksArmyWatchlist.Contains(signalSymbolBaseAsset))
            {
                var signalLocalTime = time.AddSeconds((double)_timeFrame).ToLocalTime();
                var localTimeZone = TimeZoneInfo.Local.ToString();
                var localTimeZoneParts = localTimeZone.Split(" ");
                var localTimeZoneOffset = localTimeZoneParts.Length >= 1
                    ? localTimeZoneParts[0]
                    : string.Empty;

                localTimeZoneParts = localTimeZone.Split(") ");

                var localTimeZonePlaces = localTimeZoneParts.Length >= 2
                    ? localTimeZoneParts[1]
                    : string.Empty;
                var timeFrameString = (int)_timeFrame < (int)KlineInterval.OneHour
                    ? EnumConverter.GetString(_timeFrame)
                    : EnumConverter.GetString(_timeFrame).ToUpperInvariant();

                _logger.LogInformation("********** TEST **********{NewLine1}" +
                    "{Time:yyyy-MM-dd HH:mm} {TimeZoneOffset}{NewLine1}" +
                    "{TimeZonePlaces}{NewLine2}" +
                    "{NewLine3}" +
                    "Symbol:   {Symbol}{NewLine4}" +
                    "Side:         {PositionSide}{NewLine5}" +
                    "Price:        {Price}{NewLine6}" +
                    "Interval:    {TimeFrame}",
                    Environment.NewLine,
                    signalLocalTime, localTimeZoneOffset, Environment.NewLine,
                    localTimeZonePlaces, Environment.NewLine,
                    Environment.NewLine,
                    lastCandleId.Symbol[..^4], Environment.NewLine,
                    EnumConverter.GetString(positionSide).ToUpperInvariant(), Environment.NewLine,
                    entryPrice, Environment.NewLine,
                    timeFrameString);
            }
        }

        private List<Candle> GetCandlestick(CandleId lastCandleId)
        {
            var symbol = lastCandleId.Symbol;
            var timeFrame = lastCandleId.TimeFrame;
            var candlestickId = new CandlestickId(symbol, timeFrame);
            var candlestick = new CircularTimeSeries<CandlestickId, Candle>(candlestickId, _candlestickSize);
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                var timeFrameInSeconds = (long)timeFrame;
                var lastCandleOpenTime = lastCandleId.OpenTime;
                var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(lastCandleOpenTime) / timeFrameInSeconds;
                var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * timeFrameInSeconds);
                var remainingCandles = _candlestickSize - 1;

                Enumerable.Repeat(lastCandleOpenTime, _candlestickSize)
                    .Select(openTime => lastCandleTime.AddSeconds(-(timeFrameInSeconds * remainingCandles--)))
                    .Select(openTime => new CandleId(symbol, timeFrame, openTime))
                    .ToList()
                    .ForEach(candleId =>
                    {
                        var candle = default(Candle);
                        var status = session.Read(ref candleId, ref candle);

                        if (status.Found)
                        {
                            candlestick.Add(candleId.OpenTime, candle);
                        }
                    });
            }

            return candlestick.Snapshot();
        }

        private OrderSide? GetEntrySignal(List<Candle> candlestick)
        {
            var smaLengths = new[]
            {
                Length.Five,
                Length.Ten,
                Length.Twenty,
                Length.Fifty,
                Length.OneHundred,
                Length.TwoHundred,
                //Length.ThreeHundredThirtyThree,
            };

            var rsiLengths = new[]
            {
                Length.Five,
                Length.Ten,
                Length.Twenty,
                Length.Fifty,
                Length.OneHundred,
                Length.TwoHundred,
                //Length.ThreeHundredThirtyThree,
            };

            if (candlestick.HasMissingCandles())
            {
                return null;
            }

            var smas = smaLengths.Select(length => GetSma(candlestick, length)).ToArray();
            var smasHigherTimeFrame = smaLengths.Select(length => GetSma(candlestick, length, PeriodSize.FifteenMinutes)).ToArray();
            var rsis = rsiLengths.Select(length => GetRsi(candlestick, length)).ToArray();
            var rsisHigherTimeFrame = rsiLengths.Select(length => GetRsi(candlestick, length, PeriodSize.FifteenMinutes)).ToArray();

            LogRsis(candlestick, rsiLengths, rsis);

            return /*GetTrendSignal(smas, rsis) ??*/ GetReversionSignal(smas, smasHigherTimeFrame, rsis, rsisHigherTimeFrame);
        }

        private OrderSide? GetTrendSignal(double[][] smas, double[][] rsis)
        {
            var fastRsis = rsis.Take(2);
            var fastSmas = smas.Take(2);
            var middleSmas = smas.Skip(fastSmas.Count()).Take(2);
            var slowSmas = smas.Skip(fastSmas.Count()).Skip(middleSmas.Count()).TakeLast(1);

            if (fastSmas.Any(sma => sma.Length < 2))
            {
                return null;
            }

            var penultimateFastSmas = fastSmas.Select(sma => sma[Penultimate]);
            var areSlowSmasUptrending = slowSmas.AreUptrending();
            var wereRsisOrderedFromSlowToFast = rsis.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods);
            var areFastRsisOrderedFromFastToSlow = fastRsis.PickLatestValues().AreOrderedFromFastToSlow();
            var areRsisStartingToGoUpward = wereRsisOrderedFromSlowToFast && areFastRsisOrderedFromFastToSlow;
            var arePenultimateFastSmasOrderedFromFastToSlow = penultimateFastSmas.AreOrderedFromFastToSlow();
            var areFastSmasOrderedFromFastToSlow = fastSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var areFastSmasCrossingUp = !arePenultimateFastSmasOrderedFromFastToSlow && areFastSmasOrderedFromFastToSlow;
            var areMiddleSmasOrderedFromSlowToFast = middleSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasOrderedFromFastToSlow = slowSmas.PickLatestValues().AreOrderedFromFastToSlow();

            if (areSlowSmasUptrending
                && areFastSmasCrossingUp
                && areMiddleSmasOrderedFromSlowToFast
                && areSlowSmasOrderedFromFastToSlow
                && areRsisStartingToGoUpward)
            {
                return OrderSide.Buy;
            }

            var areSlowSmasDowntrending = slowSmas.AreDowntrending();
            var wereRsisOrderedFromFastToSlow = rsis.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods);
            var areFastRsisOrderedFromSlowToFast = fastRsis.PickLatestValues().AreOrderedFromSlowToFast();
            var areRsisStartingToGoDownward = wereRsisOrderedFromFastToSlow && areFastRsisOrderedFromSlowToFast;
            var arePenultimateFastSmasOrderedFromSlowToFast = penultimateFastSmas.AreOrderedFromSlowToFast();
            var areFastSmasOrderedFromSlowToFast = fastSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var areFastSmasCrossingDown = !arePenultimateFastSmasOrderedFromSlowToFast && areFastSmasOrderedFromSlowToFast;
            var areMiddleSmasOrderedFromFastToSlow = middleSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasOrderedFromSlowToFast = slowSmas.PickLatestValues().AreOrderedFromSlowToFast();

            if (areSlowSmasDowntrending
                && areFastSmasCrossingDown
                && areMiddleSmasOrderedFromFastToSlow
                && areSlowSmasOrderedFromSlowToFast
                && areRsisStartingToGoDownward)
            {
                return OrderSide.Sell;
            }

            return null;
        }

        private OrderSide? GetReversionSignal(double[][] smas, double[][] smasHigherTimeFrame, double[][] rsis, double[][] rsisHigherTimeFrame)
        {
            var fastSmas = smas.Take(2);
            var slowSmas = smas.Skip(fastSmas.Count());

            var fastSmasHigherTimeFrame = smasHigherTimeFrame.Take(2);
            var slowSmasHigherTimeFrame = smasHigherTimeFrame.Skip(fastSmasHigherTimeFrame.Count());

            if (fastSmas.Any(sma => sma.Length < 1) || fastSmasHigherTimeFrame.Any(sma => sma.Length < 1))
            {
                return null;
            }

            var areSlowSmasDowntrending = slowSmas.TakeLast(1).AreDowntrending();
            var wereSmasOrderedFromSlowToFast = smas.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods);
            var areFastSmasOrderedFromFastToSlow = fastSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasOrderedFromSlowToFast = slowSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var wereRsisOrderedFromSlowToFast = rsis.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods);
            var areRsisOrderedFromFastToSlow = rsis.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasHigherTimeFrameDowntrending = slowSmasHigherTimeFrame.TakeLast(1).AreDowntrending();
            var wereSmasHigherTimeFrameOrderedFromSlowToFast = smasHigherTimeFrame.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods);
            var areSmasHigherTimeFrameOrderedFromSlowToFast = smasHigherTimeFrame.PickLatestValues().AreOrderedFromSlowToFast();
            var wereRsisHigherTimeFrameOrderedFromSlowToFast = rsisHigherTimeFrame.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods);

            if (areSlowSmasDowntrending
                && wereSmasOrderedFromSlowToFast
                && areFastSmasOrderedFromFastToSlow
                && areSlowSmasOrderedFromSlowToFast
                && wereRsisOrderedFromSlowToFast
                && areRsisOrderedFromFastToSlow
                && areSlowSmasHigherTimeFrameDowntrending
                && wereSmasHigherTimeFrameOrderedFromSlowToFast
                && areSmasHigherTimeFrameOrderedFromSlowToFast
                && wereRsisHigherTimeFrameOrderedFromSlowToFast)
            {
                return OrderSide.Buy;
            }

            var areSlowSmasUptrending = slowSmas.TakeLast(1).AreUptrending();
            var wereSmasOrderedFromFastToSlow = smas.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods);
            var areFastSmasOrderedFromSlowToFast = fastSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasOrderedFromFastToSlow = slowSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var wereRsisOrderedFromFastToSlow = rsis.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods);
            var areRsisOrderedFromSlowToFast = rsis.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasHigherTimeFrameUptrending = slowSmasHigherTimeFrame.TakeLast(1).AreUptrending();
            var wereSmasHigherTimeFrameOrderedFromFastToSlow = smasHigherTimeFrame.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods);
            var areSmasHigherTimeFrameOrderedFromFastToSlow = smasHigherTimeFrame.PickLatestValues().AreOrderedFromFastToSlow();
            var wereRsisHigherTimeFrameOrderedFromFastToSlow = rsisHigherTimeFrame.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods);

            if (areSlowSmasUptrending
                && wereSmasOrderedFromFastToSlow
                && areFastSmasOrderedFromSlowToFast
                && areSlowSmasOrderedFromFastToSlow
                && wereRsisOrderedFromFastToSlow
                && areRsisOrderedFromSlowToFast
                && areSlowSmasHigherTimeFrameUptrending
                && wereSmasHigherTimeFrameOrderedFromFastToSlow
                && areSmasHigherTimeFrameOrderedFromFastToSlow
                && wereRsisHigherTimeFrameOrderedFromFastToSlow)
            {
                return OrderSide.Sell;
            }

            return null;
        }

        private void LogRsis(List<Candle> candlestick, int[] lengths, double[][] rsis)
        {
            var withoutQuoteAsset = ..^4;
            var lastCandle = candlestick[Last];

            if (lastCandle.Symbol[withoutQuoteAsset] is "BTC")
            {
                var values = rsis.Select((rsi, index) => $"RSI {lengths[index]}: {rsi[Last]:F2}")
                    .Aggregate(new StringBuilder(), (builder, rsi) => builder.Append(rsi).AppendLine());

                _logger.LogTrace("{Symbol} {TimeFrame}{NewLine1}" +
                    "{OpenTime:yyyy-MM-dd HH:mm}{NewLine2}" +
                    "{Rsis}",
                    lastCandle.Symbol, EnumConverter.GetString(lastCandle.Interval), Environment.NewLine,
                    candlestick.ToArray()[Last].OpenTime.ToLocalTime(), Environment.NewLine,
                    values.ToString());
            }
        }

        private static double[] GetSma(List<Candle> candlestick, int length, PeriodSize? higherTimeFrame = null)
        {
            var quotes = candlestick.Select(candle => candle.ToQuote()).Validate();

            var warmupPeriod = length;

            if (higherTimeFrame.HasValue)
            {
                return quotes.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(warmupPeriod + length)
                    .GetSma(length)
                    .Select(result => result.Sma.GetValueOrDefault())
                    .ToArray();
            }

            return quotes.TakeLast(length + warmupPeriod)
                .GetSma(length)
                .Select(result => result.Sma.GetValueOrDefault())
                .ToArray();
        }

        private static double[] GetRsi(List<Candle> candlestick, int length, PeriodSize? higherTimeFrame = null)
        {
            var quotes = candlestick.Select(candle => candle.ToQuote()).Validate();

            var warmupPeriod = length;

            if (higherTimeFrame.HasValue)
            {
                return quotes.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(warmupPeriod + length)
                    .GetRsi(length)
                    .Select(rsi => rsi.Rsi.GetValueOrDefault())
                    .ToArray();
            }

            return quotes.TakeLast(length + warmupPeriod)
                .GetRsi(length)
                .Select(rsi => rsi.Rsi.GetValueOrDefault())
                .ToArray();
        }
    }
}
