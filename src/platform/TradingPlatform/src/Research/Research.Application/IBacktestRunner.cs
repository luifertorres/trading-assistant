using Research.Domain;
using TradingPlatform.Kernel;

namespace Research.Application;

public sealed record BacktestRequest(
    TradingVector Vector,
    SimulationConfiguration Configuration,
    DateTimeOffset? From,
    DateTimeOffset? To);

public interface IBacktestRunner
{
    Task<SimulationRunResult> RunAsync(BacktestRequest request, CancellationToken cancellationToken = default);
}
