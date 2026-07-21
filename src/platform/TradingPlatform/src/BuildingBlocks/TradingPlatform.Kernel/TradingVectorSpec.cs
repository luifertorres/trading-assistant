namespace TradingPlatform.Kernel;

/// <summary>Unique trading vector: Asset + Direction + TimeFrame + TradingLogic.</summary>
public sealed record TradingVectorSpec(
    TradingVectorId Id,
    Asset Asset,
    InstrumentId Instrument,
    TimeFrameCode TimeFrame,
    Direction Direction,
    string TradingLogic,
    IReadOnlyDictionary<string, string> Parameters)
{
    public SeriesDescriptor Series => new(Instrument, TimeFrame);
}
