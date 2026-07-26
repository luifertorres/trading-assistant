using TradingPlatform.Kernel;

namespace Research.Application;

public interface ITradingStrategyFactory
{
    ITradingStrategy Create(TradingVector vector);
}
