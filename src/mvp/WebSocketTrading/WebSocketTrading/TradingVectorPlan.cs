namespace WebSocketTrading;

public sealed record TradingVectorPlan(
    string Asset,
    IReadOnlyList<TradingVector> Vectors,
    IReadOnlyList<string> DistinctTimeframes);
