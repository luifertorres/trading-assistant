using Binance.Net.Enums;

namespace Backtesting.Mvp;

public sealed record BacktestConfig(
    string Symbol,
    KlineInterval Interval,
    decimal InitialCapital,
    decimal Quantity,
    decimal FeeBpsPerSide,
    int RsiPeriod = 14,
    decimal RsiOversold = 30m,
    decimal RsiOverbought = 70m);
