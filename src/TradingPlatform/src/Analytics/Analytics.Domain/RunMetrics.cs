using Research.Domain;
using TradingPlatform.Kernel;

namespace Analytics.Domain;

public sealed record RunMetrics(
    TradingVectorId VectorId,
    Guid RunId,
    decimal TotalReturnFraction,
    decimal MaxDrawdownFraction,
    int TradeCount,
    decimal FinalEquity);

public static class RunMetricsCalculator
{
    public static RunMetrics Summarize(SimulationRunResult run)
    {
        var init = run.Configuration.InitialCapital;
        var ret = init > 0 ? (run.FinalEquity - init) / init : 0;
        return new RunMetrics(
            run.VectorId,
            run.RunId,
            ret,
            run.MaxDrawdownFraction,
            run.Trades.Count,
            run.FinalEquity);
    }
}
