namespace WebSocketTrading;

public sealed record TradingVector(
    string Asset,
    Direction Direction,
    string Timeframe,
    TradingLogic TradingLogic);
