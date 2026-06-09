namespace TradingPlatform.Kernel;

/// <summary>Instrument, position side, timeframe, and strategy identity + parameters.</summary>
public sealed record TradingVectorSpec(
    TradingVectorId Id,
    InstrumentId Instrument,
    TimeFrameCode TimeFrame,
    PositionSide PositionSide,
    string StrategyKind,
    IReadOnlyDictionary<string, string> Parameters)
{
    public SeriesDescriptor Series => new(Instrument, TimeFrame);
}
