using Portfolio.Domain;
using Research.Domain;

namespace Portfolio.Application;

public interface IPortfolioComposer
{
    /// <summary>
    /// Greedy selection: take top runs by return, skip if underwater correlation to any picked run exceeds <paramref name="maxPairwiseCorrelation"/>.
    /// </summary>
    PortfolioDefinition ComposeDrawdownUncorrelated(
        IReadOnlyList<SimulationRunResult> runs,
        double maxPairwiseCorrelation,
        string portfolioName);
}
