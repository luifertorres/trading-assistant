namespace WebSocketTrading;

public static class SymbolNotionalFit
{
    public static bool Fits(
        decimal notionalUsd,
        decimal maxNotionalUsd,
        decimal price,
        decimal stepSize,
        decimal minQuantity,
        decimal minNotional) =>
        EntryNotionalGuard.TrySize(
            notionalUsd,
            maxNotionalUsd,
            price,
            stepSize,
            minQuantity,
            minNotional,
            out _);
}
