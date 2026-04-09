using Research.Domain;

namespace Research.Application;

public interface ISimulationRunRepository
{
    Task SaveAsync(SimulationRunResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimulationRunResult>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
}
