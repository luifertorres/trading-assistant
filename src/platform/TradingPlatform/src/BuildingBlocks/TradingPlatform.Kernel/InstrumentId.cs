namespace TradingPlatform.Kernel;

/// <summary>Opaque stable identifier for a registered instrument (allocated by the MarketData registry).</summary>
public readonly record struct InstrumentId(long Value)
{
    public override string ToString() => $"InstrumentId({Value})";
}
