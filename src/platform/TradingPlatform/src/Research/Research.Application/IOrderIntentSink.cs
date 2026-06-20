using TradingPlatform.Kernel;

namespace Research.Application;

/// <summary>Simulation path: consumes strategy intents during a backtest (Research BC).</summary>
public interface ISimulationOrderIntentSink
{
    void OnIntent(in OrderIntent intent, in OhlcBar signalBar);
    void OnBarClosed(in OhlcBar bar);
}
