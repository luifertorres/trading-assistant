using System.Globalization;

namespace Backtesting.Mvp;

public static class ReportFormatter
{
    public static string ToConsoleTable(PerformanceReport r)
    {
        var inv = CultureInfo.InvariantCulture;
        string D(decimal? v, string fmt = "N2") =>
            v.HasValue ? v.Value.ToString(fmt, inv) : "n/a";

        return $"""
            === Backtest performance ({r.Symbol}) ===
            Initial capital     : {r.InitialCapital.ToString("N2", inv)}
            Final equity        : {r.FinalEquity.ToString("N2", inv)}
            Net PnL             : {r.NetPnl.ToString("N2", inv)}
            Total return %      : {r.TotalReturnPercent.ToString("N2", inv)}%
            Max drawdown %      : {r.MaxDrawdownPercent.ToString("N2", inv)}%
            Max drawdown (abs)  : {r.MaxDrawdownAbsolute.ToString("N2", inv)}
            Sharpe (annualized) : {D(r.SharpeRatioAnnualized, "N4")}
              ({r.SharpeAssumptionNote})
            Win rate            : {(r.WinRate * 100m).ToString("N1", inv)}%
            Profit factor       : {D(r.ProfitFactor, "N4")}
            Total trades        : {r.TotalTrades}
            Average win         : {D(r.AverageWin)}
            Average loss        : {D(r.AverageLoss)}
            """;
    }
}
