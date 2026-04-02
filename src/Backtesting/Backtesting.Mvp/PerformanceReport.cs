namespace Backtesting.Mvp;

public sealed record PerformanceReport(
    string Symbol,
    decimal InitialCapital,
    decimal FinalEquity,
    decimal NetPnl,
    decimal TotalReturnPercent,
    decimal MaxDrawdownPercent,
    decimal MaxDrawdownAbsolute,
    decimal? SharpeRatioAnnualized,
    string SharpeAssumptionNote,
    decimal WinRate,
    decimal? ProfitFactor,
    int TotalTrades,
    decimal? AverageWin,
    decimal? AverageLoss);
