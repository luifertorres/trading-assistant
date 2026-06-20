namespace TradingPlatform.Kernel;

/// <summary>Logical candle series: instrument + timeframe.</summary>
public readonly record struct SeriesDescriptor(InstrumentId Instrument, TimeFrameCode TimeFrame)
{
    public void Validate()
    {
        if (Instrument.Value <= 0)
            throw new ArgumentException("Instrument id must be positive.", nameof(Instrument));
        if (string.IsNullOrWhiteSpace(TimeFrame.Value))
            throw new ArgumentException("Time frame is required.", nameof(TimeFrame));
    }
}
