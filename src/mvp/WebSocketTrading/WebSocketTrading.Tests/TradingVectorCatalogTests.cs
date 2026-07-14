using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class TradingVectorCatalogTests
{
    private static TradingVector V(
        string asset = "DOGEUSDT",
        Direction direction = Direction.Short,
        string timeframe = "OneDay",
        TradingLogic logic = TradingLogic.Sma200Sma5) =>
        new(asset, direction, timeframe, logic);

    [Fact]
    public void Build_WhenEmpty_Throws()
    {
        var act = () => TradingVectorCatalog.Build([]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one trading vector*");
    }

    [Fact]
    public void Build_WhenMultipleAssets_Throws()
    {
        var vectors = new[]
        {
            V(asset: "DOGEUSDT"),
            V(asset: "BTCUSDT", direction: Direction.Long)
        };

        var act = () => TradingVectorCatalog.Build(vectors);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*same Asset*");
    }

    [Fact]
    public void Build_WhenDuplicateIdentity_Throws()
    {
        var vectors = new[]
        {
            V(timeframe: "OneDay"),
            V(timeframe: "OneDay")
        };

        var act = () => TradingVectorCatalog.Build(vectors);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unique*");
    }

    [Fact]
    public void Build_WhenMixedTimeframesSameAsset_ReturnsPlanWithDistinctTimeframes()
    {
        var vectors = new[]
        {
            V(direction: Direction.Short, timeframe: "OneDay"),
            V(direction: Direction.Long, timeframe: "OneMinute")
        };

        var plan = TradingVectorCatalog.Build(vectors);

        plan.Asset.Should().Be("DOGEUSDT");
        plan.Vectors.Should().HaveCount(2);
        plan.DistinctTimeframes.Should().BeEquivalentTo(["OneDay", "OneMinute"]);
    }

    [Fact]
    public void Build_WhenSameTimeframeMultipleDirections_ReturnsSingleTimeframe()
    {
        var vectors = new[]
        {
            V(direction: Direction.Short, timeframe: "OneDay"),
            V(direction: Direction.Long, timeframe: "OneDay")
        };

        var plan = TradingVectorCatalog.Build(vectors);

        plan.DistinctTimeframes.Should().Equal("OneDay");
        plan.Vectors.Should().HaveCount(2);
    }
}
