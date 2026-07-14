using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class QuantitySizerTests
{
    [Fact]
    public void SizeFromNotional_RoundsToStepSize()
    {
        var quantity = QuantitySizer.SizeFromNotional(
            notionalUsd: 10m,
            price: 0.15m,
            stepSize: 1m,
            minQuantity: 1m,
            minNotional: 5m);

        quantity.Should().Be(67m);
    }

    [Fact]
    public void SizeFromNotional_EnforcesMinNotional()
    {
        var quantity = QuantitySizer.SizeFromNotional(
            notionalUsd: 3m,
            price: 0.15m,
            stepSize: 1m,
            minQuantity: 1m,
            minNotional: 5m);

        quantity.Should().Be(34m);
    }

    [Fact]
    public void SizeFromNotional_ClampsToMinQuantity()
    {
        var quantity = QuantitySizer.SizeFromNotional(
            notionalUsd: 1m,
            price: 100m,
            stepSize: 0.001m,
            minQuantity: 0.01m,
            minNotional: 5m);

        quantity.Should().Be(0.05m);
    }
}
