using Analytics.Domain;
using Research.Domain;

namespace Analytics.Application;

public interface IRunAnalytics
{
    IReadOnlyList<RunMetrics> RankByReturn(IReadOnlyList<SimulationRunResult> runs);

    /// <summary>Pairwise Pearson correlation of synchronized underwater (drawdown) fractions.</summary>
    IReadOnlyDictionary<(Guid RunIdA, Guid RunIdB), double> UnderwaterCorrelationMatrix(
        IReadOnlyList<SimulationRunResult> runs);
}
