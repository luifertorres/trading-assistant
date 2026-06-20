namespace TradingPlatform.Kernel;

public readonly record struct TradingVectorId(Guid Value)
{
    public static TradingVectorId New() => new(Guid.NewGuid());

    public static TradingVectorId From(Guid g) => new(g);
}
