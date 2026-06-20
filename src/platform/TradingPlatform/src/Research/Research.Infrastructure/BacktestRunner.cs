using MarketData.Application;
using Research.Application;
using Research.Domain;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

public sealed class BacktestRunner(
    ICandleSeriesReader candles,
    ITradingStrategyFactory strategies) : IBacktestRunner
{
    public async Task<SimulationRunResult> RunAsync(BacktestRequest request, CancellationToken cancellationToken = default)
    {
        var series = request.Vector.Series;
        var bars = await candles.ReadAsync(series, request.From, request.To, cancellationToken).ConfigureAwait(false);
        if (bars.Count == 0)
        {
            return new SimulationRunResult(
                request.Vector.Id,
                Guid.NewGuid(),
                request.Configuration,
                [],
                [],
                request.Configuration.InitialCapital,
                0);
        }

        var sink = new SimulationOrderIntentSink(request.Configuration, request.Vector.PositionSide);
        var strategy = strategies.Create(request.Vector);
        for (var i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            strategy.OnBar(new BarProcessingContext
            {
                Bar = bar,
                BarIndex = i,
                Vector = request.Vector,
                Sink = sink
            });
            sink.OnBarClosed(in bar);
        }

        var equity = sink.Equity;
        var final = equity.Count > 0 ? equity[^1].Equity : request.Configuration.InitialCapital;
        var maxDd = SimulationOrderIntentSink.MaxDrawdownFraction(equity);
        return new SimulationRunResult(
            request.Vector.Id,
            Guid.NewGuid(),
            request.Configuration,
            sink.Trades,
            equity,
            final,
            maxDd);
    }
}
