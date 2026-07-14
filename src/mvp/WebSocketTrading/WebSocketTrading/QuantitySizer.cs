namespace WebSocketTrading;

public static class QuantitySizer
{
    public static decimal SizeFromNotional(
        decimal notionalUsd,
        decimal price,
        decimal stepSize,
        decimal minQuantity,
        decimal minNotional)
    {
        var quantity = notionalUsd / price;

        if (quantity * price < minNotional)
            quantity = minNotional / price;

        quantity = Math.Max(quantity, minQuantity);

        var remainder = (quantity - minQuantity) % stepSize;
        if (remainder > 0)
        {
            quantity -= remainder;
            quantity += stepSize;
        }

        return quantity;
    }
}
