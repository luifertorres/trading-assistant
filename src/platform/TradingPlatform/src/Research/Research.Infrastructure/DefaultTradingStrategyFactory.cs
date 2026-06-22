using Research.Application;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

public sealed class DefaultTradingStrategyFactory : ITradingStrategyFactory
{
    public ITradingStrategy Create(TradingVectorSpec vector) =>
        vector.StrategyKind switch
        {
            "FixedWindow" => new FixedWindowStrategy(),
            "Rsi5Extreme" => new Rsi5ExtremeStrategy(),
            _ => throw new NotSupportedException($"Unknown strategy kind: {vector.StrategyKind}")
        };
}
