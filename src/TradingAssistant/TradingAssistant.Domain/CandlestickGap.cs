namespace TradingAssistant
{
    public readonly record struct CandlestickGap(List<TimeSpan> Durations, DateTime PreviousTime)
    {
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


