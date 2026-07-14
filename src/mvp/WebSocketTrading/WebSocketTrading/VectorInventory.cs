namespace WebSocketTrading;

public sealed class VectorInventory
{
    private decimal _openQuantity;

    public decimal OpenQuantity => _openQuantity;

    public void AddFill(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Fill quantity must be positive.");

        _openQuantity += quantity;
    }

    public void Seed(decimal quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Seed quantity cannot be negative.");

        _openQuantity = quantity;
    }

    public decimal ConsumeForExit()
    {
        var quantity = _openQuantity;
        _openQuantity = 0m;
        return quantity;
    }
}
