namespace WebSocketTrading;

public static class EntryNotionalGuard
{
    public static bool TrySize(
        decimal notionalUsd,
        decimal maxNotionalUsd,
        decimal price,
        decimal stepSize,
        decimal minQuantity,
        decimal minNotional,
        out decimal quantity)
    {
        quantity = QuantitySizer.SizeFromNotional(
            notionalUsd,
            price,
            stepSize,
            minQuantity,
            minNotional);

        if (quantity * price <= maxNotionalUsd)
            return true;

        quantity = 0m;
        return false;
    }
}
