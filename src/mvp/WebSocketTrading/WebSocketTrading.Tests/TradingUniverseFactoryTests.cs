using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class TradingUniverseFactoryTests
{
    [Fact]
    public void BuildLongShort_WhenEmpty_Throws()
    {
        var act = () => TradingUniverseFactory.BuildLongShort(
            [],
            "FiveMinutes",
            TradingLogic.Sma200Sma5);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one symbol*");
    }

    [Fact]
    public void BuildLongShort_ForNSymbols_EmitsTwoVectorsPerSymbol()
    {
        var vectors = TradingUniverseFactory.BuildLongShort(
            ["BTCUSDT", "ETHUSDT", "DOGEUSDT"],
            "FiveMinutes",
            TradingLogic.Sma200Sma5);

        vectors.Should().HaveCount(6);
        vectors.Should().Contain(v =>
            v.Asset == "BTCUSDT" && v.Direction == Direction.Long && v.Timeframe == "FiveMinutes");
        vectors.Should().Contain(v =>
            v.Asset == "BTCUSDT" && v.Direction == Direction.Short && v.Timeframe == "FiveMinutes");
        vectors.Should().Contain(v =>
            v.Asset == "ETHUSDT" && v.Direction == Direction.Long && v.Timeframe == "FiveMinutes");
        vectors.Should().Contain(v =>
            v.Asset == "ETHUSDT" && v.Direction == Direction.Short && v.Timeframe == "FiveMinutes");
    }
}
