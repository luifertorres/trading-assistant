using Research.Domain;

namespace Research.Application;

/// <summary>Machine-readable backtest gate for live arming.</summary>
public sealed record BacktestVerdict(
    string Symbol,
    string StrategyKind,
    string TimeFrame,
    Guid RunId,
    bool Pass,
    int TradeCount,
    decimal TotalReturnFraction,
    decimal MaxDrawdownFraction,
    decimal ProfitFactor,
    string? FailReason,
    DateTimeOffset EvaluatedAtUtc);

public static class BacktestVerdictEvaluator
{
    public const int MinTrades = 3;
    public const decimal MinReturnFraction = 0m;
    public const decimal MaxDrawdownCap = 0.50m;
    public const decimal MinProfitFactor = 1.0m;

    public static BacktestVerdict Evaluate(
        string symbol,
        string strategyKind,
        string timeFrame,
        SimulationRunResult run)
    {
        var init = run.Configuration.InitialCapital;
        var ret = init > 0 ? (run.FinalEquity - init) / init : 0m;
        var pf = ProfitFactor(run.Trades);
        var reasons = new List<string>();

        if (run.Trades.Count < MinTrades)
            reasons.Add($"trades {run.Trades.Count} < {MinTrades}");
        if (ret <= MinReturnFraction)
            reasons.Add($"return {ret:P2} <= {MinReturnFraction:P2}");
        if (run.MaxDrawdownFraction > MaxDrawdownCap)
            reasons.Add($"maxDD {run.MaxDrawdownFraction:P2} > {MaxDrawdownCap:P2}");
        if (pf < MinProfitFactor)
            reasons.Add($"profitFactor {pf:F2} < {MinProfitFactor:F2}");

        var pass = reasons.Count == 0;
        return new BacktestVerdict(
            symbol,
            strategyKind,
            timeFrame,
            run.RunId,
            pass,
            run.Trades.Count,
            ret,
            run.MaxDrawdownFraction,
            pf,
            pass ? null : string.Join("; ", reasons),
            DateTimeOffset.UtcNow);
    }

    public static decimal ProfitFactor(IReadOnlyList<TradeRecord> trades)
    {
        var wins = trades.Where(t => t.NetPnl > 0).Sum(t => t.NetPnl);
        var losses = trades.Where(t => t.NetPnl < 0).Sum(t => -t.NetPnl);
        if (losses <= 0)
            return wins > 0 ? 999m : 0m;
        return wins / losses;
    }
}
