namespace WebSocketTrading;

public sealed record TradingVectorPlan(
    IReadOnlyList<string> Assets,
    IReadOnlyList<TradingVector> Vectors,
    IReadOnlyList<string> DistinctTimeframes,
    IReadOnlyList<AssetTimeframe> DistinctAssetTimeframes);
