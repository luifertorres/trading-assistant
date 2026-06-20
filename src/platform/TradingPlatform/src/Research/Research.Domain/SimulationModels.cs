using TradingPlatform.Kernel;

namespace Research.Domain;

public readonly record struct EquityPoint(DateTimeOffset At, decimal Equity);

public sealed record TradeRecord(
    DateTimeOffset EntryTime,
    DateTimeOffset ExitTime,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal Quantity,
    decimal GrossPnl,
    decimal Fees,
    decimal NetPnl);

public sealed record SimulationConfiguration(
    decimal InitialCapital,
    decimal FeeBpsPerSide,
    decimal PositionNotionalFraction);

public sealed record SimulationRunResult(
    TradingVectorId VectorId,
    Guid RunId,
    SimulationConfiguration Configuration,
    IReadOnlyList<TradeRecord> Trades,
    IReadOnlyList<EquityPoint> Equity,
    decimal FinalEquity,
    decimal MaxDrawdownFraction);
