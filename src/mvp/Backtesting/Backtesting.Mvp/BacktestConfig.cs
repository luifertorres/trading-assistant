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
    decimal RsiOverbought = 70m,
    /// <summary>When true, <see cref="Quantity"/> is ignored; size each entry from <see cref="PositionNotionalFractionOfInitial"/> and exchange floors.</summary>
    bool SizePositionByInitialCapitalFraction = false,
    decimal PositionNotionalFractionOfInitial = 0.02m,
    decimal MinNotionalUsd = 134m,
    decimal MinOrderQuantityBtc = 0.002m,
    decimal QuantityStepBtc = 0.001m);
