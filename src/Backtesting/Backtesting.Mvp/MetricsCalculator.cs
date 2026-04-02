using Binance.Net.Enums;

namespace Backtesting.Mvp;

public static class MetricsCalculator
{
    private const double Epsilon = 1e-12;

    public static PerformanceReport Build(BacktestRunResult run)
    {
        var cfg = run.Config;
        var curve = run.EquityCurve;
        var trades = run.Trades;

        if (curve.Count == 0)
        {
            return new PerformanceReport(
                cfg.Symbol,
                cfg.InitialCapital,
                cfg.InitialCapital,
                0m,
                0m,
                0m,
                0m,
                null,
                "N/A (empty run)",
                0m,
                null,
                0,
                null,
                null);
        }

        var final = curve[^1].Equity;
        var netPnl = final - cfg.InitialCapital;
        var totalReturnPct = cfg.InitialCapital > 0
            ? netPnl / cfg.InitialCapital * 100m
            : 0m;

        var (maxDdPct, maxDdAbs) = MaxDrawdown(curve);

        var (sharpe, sharpeNote) = Sharpe(curve, cfg.Interval);

        var wins = trades.Where(t => t.NetPnl > 0).ToList();
        var losses = trades.Where(t => t.NetPnl < 0).ToList();
        var winRate = trades.Count > 0
            ? (decimal)wins.Count / trades.Count
            : 0m;

        var sumWins = wins.Sum(t => t.NetPnl);
        var sumLosses = losses.Sum(t => t.NetPnl);
        decimal? profitFactor = null;
        if (losses.Count > 0 && sumLosses != 0)
            profitFactor = sumWins / Math.Abs(sumLosses);

        decimal? avgWin = wins.Count > 0 ? wins.Average(t => t.NetPnl) : null;
        decimal? avgLoss = losses.Count > 0 ? losses.Average(t => t.NetPnl) : null;

        return new PerformanceReport(
            cfg.Symbol,
            cfg.InitialCapital,
            final,
            netPnl,
            totalReturnPct,
            maxDdPct,
            maxDdAbs,
            sharpe,
            sharpeNote,
            winRate,
            profitFactor,
            trades.Count,
            avgWin,
            avgLoss);
    }

    private static (decimal Percent, decimal Absolute) MaxDrawdown(IReadOnlyList<EquityPoint> curve)
    {
        decimal peak = curve[0].Equity;
        var maxDdAbs = 0m;
        foreach (var p in curve)
        {
            var eq = p.Equity;
            if (eq > peak)
                peak = eq;
            var dd = peak - eq;
            if (dd > maxDdAbs)
                maxDdAbs = dd;
        }

        var maxDdPct = peak > 0 ? maxDdAbs / peak * 100m : 0m;
        return (maxDdPct, maxDdAbs);
    }

    private static (decimal? Ratio, string Note) Sharpe(IReadOnlyList<EquityPoint> curve, KlineInterval interval)
    {
        if (curve.Count < 3)
            return (null, "Need at least 3 equity points");

        var rets = new List<double>();
        for (var i = 1; i < curve.Count; i++)
        {
            var prev = (double)curve[i - 1].Equity;
            var curr = (double)curve[i].Equity;
            if (Math.Abs(prev) < Epsilon)
                continue;
            rets.Add((curr - prev) / prev);
        }

        if (rets.Count < 2)
            return (null, "Insufficient return samples");

        var mean = rets.Average();
        var variance = rets.Sum(r => (r - mean) * (r - mean)) / (rets.Count - 1);
        var std = Math.Sqrt(variance);
        if (std < Epsilon)
            return (null, "Zero volatility of returns");

        var barsPerYear = interval.BarsPerYear();
        var sharpe = (mean / std) * Math.Sqrt(barsPerYear);
        var note = $"Per-bar simple returns on equity; sqrt(bars/year) with bars/year ≈ {barsPerYear:F0} from {interval}";
        return ((decimal)sharpe, note);
    }
}
