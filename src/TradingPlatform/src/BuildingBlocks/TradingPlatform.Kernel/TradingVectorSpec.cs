namespace TradingPlatform.Kernel;

/// <summary>Symbol, position side, timeframe, and strategy identity + parameters.</summary>
public sealed record TradingVectorSpec(
    TradingVectorId Id,
    string Symbol,
    TimeFrameCode TimeFrame,
    PositionSide PositionSide,
    string StrategyKind,
    IReadOnlyDictionary<string, string> Parameters)
{
    public SeriesDescriptor Series => new(Symbol, TimeFrame);
}
