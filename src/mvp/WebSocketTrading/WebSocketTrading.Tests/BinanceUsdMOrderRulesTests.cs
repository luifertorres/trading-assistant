using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class BinanceUsdMOrderRulesTests
{
    [Fact]
    public void ReduceOnlyParameter_WhenHedgeMode_ReturnsNull()
    {
        BinanceUsdMOrderRules.ReduceOnlyParameter(hedgeMode: true).Should().BeNull();
    }

    [Fact]
    public void ReduceOnlyParameter_WhenOneWayMode_ReturnsTrue()
    {
        BinanceUsdMOrderRules.ReduceOnlyParameter(hedgeMode: false).Should().BeTrue();
    }
}
