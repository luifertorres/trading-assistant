namespace TradingPlatform.Kernel;

/// <summary>Logical candle series: symbol + timeframe. Physical table name is a MarketData implementation detail.</summary>
public readonly record struct SeriesDescriptor(string Symbol, TimeFrameCode TimeFrame)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            throw new ArgumentException("Symbol is required.", nameof(Symbol));
        if (string.IsNullOrWhiteSpace(TimeFrame.Value))
            throw new ArgumentException("Time frame is required.", nameof(TimeFrame));
    }
}
