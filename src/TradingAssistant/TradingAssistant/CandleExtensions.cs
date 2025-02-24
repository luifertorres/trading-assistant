using Skender.Stock.Indicators;

namespace TradingAssistant
{
    internal static class CandleExtensions
    {
        internal static Index Last = ^1;
        internal static Index WithoutQuoteAsset = ^4;

        internal static bool IsCorrelatedWith(this List<Candle> candlestickA, List<Candle> candlestickB)
        {
            if (candlestickB.Count == 0)
            {
                return true;
            }

            if (HasMissingCandles(candlestickA) || HasMissingCandles(candlestickB))
            {
                return true;
            }

            var lookbackPeriods = Length.TwoHundred;
            var correlation = GetCorrelation(candlestickA, candlestickB, lookbackPeriods);

            return correlation.Length == 0 || Math.Abs(correlation[Last]) > 0.1;
        }

        internal static bool HasMissingCandles(this List<Candle> candlestick)
        {
            var firstCandle = candlestick[0];
            var initialGap = new CandlestickGap([], firstCandle.OpenTime);
            var timeFrameSpan = TimeSpan.FromSeconds((double)candlestick[Last].Interval);
            var gap = candlestick.Aggregate(initialGap, CandlestickGap.UpdateGapFromCandle);
            var hasMissingCandles = gap.Durations.Exists(duration => duration > timeFrameSpan);

            return hasMissingCandles;
        }

        private static double[] GetCorrelation(this List<Candle> candlestickA,
            List<Candle> candlestickB,
            int length,
            PeriodSize? higherTimeFrame = null)
        {
            var warmupPeriod = 0;
            var requiredLength = warmupPeriod + length;

            if (candlestickA.Count < requiredLength || candlestickB.Count < candlestickA.Count)
            {
                return [];
            }

            var quotesA = candlestickA.Select(ToQuote).Validate();
            var quotesB = candlestickB.Select(ToQuote).Validate();

            if (higherTimeFrame.HasValue)
            {
                quotesB = quotesB.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(requiredLength);

                return quotesA.Aggregate(higherTimeFrame.Value)
                    .Validate()
                    .TakeLast(requiredLength)
                    .GetCorrelation(quotesB, length)
                    .Select(result => result.Correlation.GetValueOrDefault())
                    .ToArray();
            }

            quotesB = quotesB.TakeLast(requiredLength);

            return quotesA.TakeLast(requiredLength)
                .GetCorrelation(quotesB, length)
                .Select(result => result.Correlation.GetValueOrDefault())
                .ToArray();
        }

        internal static Quote ToQuote(this Candle candle)
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
