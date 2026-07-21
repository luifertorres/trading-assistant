using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public sealed class VectorInventoryTests
{
    [Fact]
    public void AddFill_IncreasesOpenQuantity()
    {
        var inventory = new VectorInventory();

        inventory.AddFill(0.5m);

        inventory.OpenQuantity.Should().Be(0.5m);
    }

    [Fact]
    public void ConsumeForExit_ReturnsTrackedAndClears()
    {
        var inventory = new VectorInventory();
        inventory.AddFill(1.25m);

        var exitQty = inventory.ConsumeForExit();

        exitQty.Should().Be(1.25m);
        inventory.OpenQuantity.Should().Be(0m);
    }

    [Fact]
    public void SeparateInventories_AreIndependent()
    {
        var first = new VectorInventory();
        var second = new VectorInventory();
        first.AddFill(2m);
        second.AddFill(3m);

        first.ConsumeForExit().Should().Be(2m);
        second.OpenQuantity.Should().Be(3m);
    }

    [Fact]
    public void Seed_SetsOpenQuantity()
    {
        var inventory = new VectorInventory();

        inventory.Seed(1.5m);

        inventory.OpenQuantity.Should().Be(1.5m);
    }
}
