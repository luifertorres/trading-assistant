using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class VectorInventoryTests
{
    [Fact]
    public void AddFill_AccumulatesOpenQuantity()
    {
        var inventory = new VectorInventory();

        inventory.AddFill(10m);
        inventory.AddFill(5m);

        inventory.OpenQuantity.Should().Be(15m);
    }

    [Fact]
    public void ConsumeForExit_ReturnsOpenQuantityAndZeros()
    {
        var inventory = new VectorInventory();
        inventory.AddFill(12m);

        var exitQty = inventory.ConsumeForExit();

        exitQty.Should().Be(12m);
        inventory.OpenQuantity.Should().Be(0m);
    }

    [Fact]
    public void Seed_SetsOpenQuantity()
    {
        var inventory = new VectorInventory();

        inventory.Seed(25m);

        inventory.OpenQuantity.Should().Be(25m);
    }

    [Fact]
    public void ConsumeForExit_WhenEmpty_ReturnsZero()
    {
        var inventory = new VectorInventory();

        inventory.ConsumeForExit().Should().Be(0m);
    }
}
