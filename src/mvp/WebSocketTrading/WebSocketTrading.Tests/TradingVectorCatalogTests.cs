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
    public void Build_WhenMultipleAssets_ReturnsPlanWithDistinctAssets()
    {
        var vectors = new[]
        {
            V(asset: "DOGEUSDT"),
            V(asset: "BTCUSDT", direction: Direction.Long)
        };

        var plan = TradingVectorCatalog.Build(vectors);

        plan.Assets.Should().BeEquivalentTo(["DOGEUSDT", "BTCUSDT"]);
        plan.Vectors.Should().HaveCount(2);
    }

    [Fact]
    public void Build_WhenMultipleAssets_ReturnsDistinctAssetTimeframes()
    {
        var vectors = new[]
        {
            V(asset: "DOGEUSDT", timeframe: "FiveMinutes"),
            V(asset: "BTCUSDT", direction: Direction.Long, timeframe: "FiveMinutes")
        };

        var plan = TradingVectorCatalog.Build(vectors);

        plan.DistinctAssetTimeframes.Should().BeEquivalentTo(
        [
            new AssetTimeframe("DOGEUSDT", "FiveMinutes"),
            new AssetTimeframe("BTCUSDT", "FiveMinutes")
        ]);
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

        plan.Assets.Should().Equal("DOGEUSDT");
        plan.Vectors.Should().HaveCount(2);
        plan.DistinctTimeframes.Should().BeEquivalentTo(["OneDay", "OneMinute"]);
        plan.DistinctAssetTimeframes.Should().BeEquivalentTo(
        [
            new AssetTimeframe("DOGEUSDT", "OneDay"),
            new AssetTimeframe("DOGEUSDT", "OneMinute")
        ]);
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
