using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class EntryNotionalGuardTests
{
    [Fact]
    public void TrySize_WhenNotionalWithinMax_ReturnsQuantity()
    {
        var ok = EntryNotionalGuard.TrySize(
            notionalUsd: 5m,
            maxNotionalUsd: 5.5m,
            price: 0.15m,
            stepSize: 1m,
            minQuantity: 1m,
            minNotional: 5m,
            out var quantity);

        ok.Should().BeTrue();
        quantity.Should().Be(34m);
        (quantity * 0.15m).Should().BeLessThanOrEqualTo(5.5m);
    }

    [Fact]
    public void TrySize_WhenRoundUpExceedsMax_ReturnsFalse()
    {
        var ok = EntryNotionalGuard.TrySize(
            notionalUsd: 5m,
            maxNotionalUsd: 5.5m,
            price: 100m,
            stepSize: 0.001m,
            minQuantity: 0.06m,
            minNotional: 5m,
            out var quantity);

        ok.Should().BeFalse();
        quantity.Should().Be(0m);
    }
}
