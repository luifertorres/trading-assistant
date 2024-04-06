namespace TradingAssistant
{
    internal record struct CandlestickGap(List<TimeSpan> Durations, DateTime PreviousTime)
    {
        internal static CandlestickGap UpdateGapFromCandle(CandlestickGap gap, Candle candle)
        {
            var durationBetweenCandles = (candle.OpenTime - gap.PreviousTime).Duration();

            gap.Durations.Add(durationBetweenCandles);
            gap.PreviousTime = candle.OpenTime;

            return gap;
        }

        public static implicit operator (List<TimeSpan> Durations, DateTime PreviousTime)(CandlestickGap value)
        {
            return (value.Durations, value.PreviousTime);
        }

        public static implicit operator CandlestickGap((List<TimeSpan> Durations, DateTime PreviousTime) value)
        {
            return new CandlestickGap(value.Durations, value.PreviousTime);
        }
    }
}
