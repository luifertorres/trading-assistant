using TradingPlatform.Kernel;

namespace Research.Application;

public sealed class BarProcessingContext
{
    public required OhlcBar Bar { get; init; }
    public required int BarIndex { get; init; }
    public required TradingVectorSpec Vector { get; init; }
    public required ISimulationOrderIntentSink Sink { get; init; }
}
