using Research.Application;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

public sealed class DefaultTradingStrategyFactory : ITradingStrategyFactory
{
    public ITradingStrategy Create(TradingVector vector) =>
        vector.TradingLogic switch
        {
            "FixedWindow" => new FixedWindowStrategy(),
            "Rsi5Extreme" => new Rsi5ExtremeStrategy(),
            "Rsi5ExtremeSma200" => new Rsi5ExtremeStrategy(requireSma200DirectionBias: true),
            "Sma200Sma5" => new Sma200Sma5Strategy(),
            _ => throw new NotSupportedException($"Unknown trading logic: {vector.TradingLogic}")
        };
}
