using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class SymbolNotionalFitTests
{
    [Fact]
    public void Fits_WhenSizedNotionalWithinMax_ReturnsTrue()
    {
        SymbolNotionalFit.Fits(
            notionalUsd: 5m,
            maxNotionalUsd: 5.5m,
            price: 0.15m,
            stepSize: 1m,
            minQuantity: 1m,
            minNotional: 5m).Should().BeTrue();
    }

    [Fact]
    public void Fits_WhenMinQuantityTimesPriceExceedsMax_ReturnsFalse()
    {
        SymbolNotionalFit.Fits(
            notionalUsd: 5m,
            maxNotionalUsd: 5.5m,
            price: 100m,
            stepSize: 0.001m,
            minQuantity: 0.06m,
            minNotional: 5m).Should().BeFalse();
    }

    [Fact]
    public void Fits_WhenExchangeMinNotionalExceedsMax_ReturnsFalse()
    {
        SymbolNotionalFit.Fits(
            notionalUsd: 5m,
            maxNotionalUsd: 5.5m,
            price: 1m,
            stepSize: 1m,
            minQuantity: 1m,
            minNotional: 6m).Should().BeFalse();
    }
}
