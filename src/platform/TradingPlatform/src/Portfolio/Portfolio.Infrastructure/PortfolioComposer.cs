using Analytics.Application;
using Portfolio.Application;
using Portfolio.Domain;
using Research.Domain;

namespace Portfolio.Infrastructure;

public sealed class PortfolioComposer(IRunAnalytics analytics) : IPortfolioComposer
{
    public PortfolioDefinition ComposeDrawdownUncorrelated(
        IReadOnlyList<SimulationRunResult> runs,
        double maxPairwiseCorrelation,
        string portfolioName)
    {
        if (runs.Count == 0)
            return new PortfolioDefinition(Guid.NewGuid(), portfolioName, [], DateTimeOffset.UtcNow);

        var ranked = analytics.RankByReturn(runs);
        var runById = runs.ToDictionary(r => r.RunId);
        var matrix = analytics.UnderwaterCorrelationMatrix(runs);
        var picked = new List<SimulationRunResult>();
        foreach (var m in ranked)
        {
            if (!runById.TryGetValue(m.RunId, out var full))
                continue;
            var ok = true;
            foreach (var existing in picked)
            {
                if (matrix.TryGetValue((full.RunId, existing.RunId), out var c) && Math.Abs(c) > maxPairwiseCorrelation)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
                picked.Add(full);
        }

        if (picked.Count == 0)
            picked.Add(runById[ranked[0].RunId]);

        var w = 1m / picked.Count;
        var members = picked
            .Select(r => new PortfolioMember(r.VectorId, w))
            .ToList();
        return new PortfolioDefinition(Guid.NewGuid(), portfolioName, members, DateTimeOffset.UtcNow);
    }
}
