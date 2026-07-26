using Portfolio.Domain;
using Research.Application;
using TradingPlatform.Kernel;

namespace Execution.Application;

/// <summary>
/// Uses the same <see cref="ITradingStrategyFactory"/> as research; routes intents to <see cref="ILiveOrderIntentSink"/> instead of simulation.
/// </summary>
public sealed class PortfolioExecutionRouter(
    ITradingStrategyFactory strategies,
    ILiveOrderIntentSink liveSink)
{
    public async Task ExecuteOneShotAsync(
        PortfolioDefinition portfolio,
        IReadOnlyDictionary<TradingVectorId, TradingVector> vectorById,
        IReadOnlyList<OhlcBar> bars,
        TradingVectorId activeVectorId,
        CancellationToken cancellationToken = default)
    {
        if (!portfolio.Members.Any(m => m.VectorId == activeVectorId))
            throw new InvalidOperationException("Vector not in portfolio.");
        if (!vectorById.TryGetValue(activeVectorId, out var spec))
            throw new InvalidOperationException("Unknown vector spec.");

        var strategy = strategies.Create(spec);
        LiveStrategySinkAdapter adapter = new(liveSink);
        for (var i = 0; i < bars.Count; i++)
        {
            strategy.OnBar(new BarProcessingContext
            {
                Bar = bars[i],
                BarIndex = i,
                Vector = spec,
                Sink = adapter
            });
            await adapter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class LiveStrategySinkAdapter(ILiveOrderIntentSink live) : ISimulationOrderIntentSink
    {
        private readonly Queue<(OrderIntent intent, OhlcBar bar)> _pending = new();

        public void OnIntent(in OrderIntent intent, in OhlcBar signalBar) =>
            _pending.Enqueue((intent, signalBar));

        public void OnBarClosed(in OhlcBar bar)
        {
        }

        public async Task FlushAsync(CancellationToken ct)
        {
            while (_pending.Count > 0)
            {
                var (i, _) = _pending.Dequeue();
                await live.OnIntentAsync(i, ct).ConfigureAwait(false);
            }
        }
    }
}
