namespace Backtesting.Mvp;

public sealed record BacktestRunResult(
    BacktestConfig Config,
    IReadOnlyList<TradeRecord> Trades,
    IReadOnlyList<EquityPoint> EquityCurve);
