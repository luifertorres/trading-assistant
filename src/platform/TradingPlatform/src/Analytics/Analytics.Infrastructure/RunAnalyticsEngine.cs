using Analytics.Application;
using Analytics.Domain;
using Research.Domain;

namespace Analytics.Infrastructure;

public sealed class RunAnalyticsEngine : IRunAnalytics
{
    public IReadOnlyList<RunMetrics> RankByReturn(IReadOnlyList<SimulationRunResult> runs) =>
        runs
            .Select(RunMetricsCalculator.Summarize)
            .OrderByDescending(m => m.TotalReturnFraction)
            .ToList();

    public IReadOnlyDictionary<(Guid RunIdA, Guid RunIdB), double> UnderwaterCorrelationMatrix(
        IReadOnlyList<SimulationRunResult> runs)
    {
        var dict = new Dictionary<(Guid, Guid), double>();
        var uw = runs
            .Select(r => (r.RunId, Series: UnderwaterFractions(r.Equity)))
            .ToList();
        for (var i = 0; i < uw.Count; i++)
        {
            for (var j = i + 1; j < uw.Count; j++)
            {
                var c = Pearson(uw[i].Series, uw[j].Series);
                dict[(uw[i].RunId, uw[j].RunId)] = c;
                dict[(uw[j].RunId, uw[i].RunId)] = c;
            }
        }

        return dict;
    }

    private static IReadOnlyList<double> UnderwaterFractions(IReadOnlyList<EquityPoint> equity)
    {
        if (equity.Count == 0)
            return [];
        var peak = (double)equity[0].Equity;
        var list = new List<double>(equity.Count);
        foreach (var p in equity)
        {
            var e = (double)p.Equity;
            if (e > peak)
                peak = e;
            list.Add(peak > 0 ? (peak - e) / peak : 0);
        }

        return list;
    }

    private static double Pearson(IReadOnlyList<double> u, IReadOnlyList<double> v)
    {
        var n = Math.Min(u.Count, v.Count);
        if (n < 2)
            return 0;
        double mu = 0, mv = 0;
        for (var i = 0; i < n; i++)
        {
            mu += u[i];
            mv += v[i];
        }

        mu /= n;
        mv /= n;
        double num = 0, du = 0, dv = 0;
        for (var i = 0; i < n; i++)
        {
            var a = u[i] - mu;
            var b = v[i] - mv;
            num += a * b;
            du += a * a;
            dv += b * b;
        }

        if (du < 1e-18 || dv < 1e-18)
            return 0;
        return num / Math.Sqrt(du * dv);
    }
}
