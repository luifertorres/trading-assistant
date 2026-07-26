using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public sealed class TradingVectorIdentityTests
{
    [Fact]
    public void EnsureUnique_RejectsDuplicateFourTuple()
    {
        var asset = Asset.FromUsdmExchangeSymbol("BTCUSDT");
        var instrument = new InstrumentId(1);
        var tf = TimeFrameCode.Day1;
        var vectors = new[]
        {
            new TradingVector(
                TradingVectorId.New(),
                asset,
                instrument,
                tf,
                Direction.Long,
                "Sma200Sma5",
                new Dictionary<string, string>()),
            new TradingVector(
                TradingVectorId.New(),
                asset,
                instrument,
                tf,
                Direction.Long,
                "Sma200Sma5",
                new Dictionary<string, string>())
        };

        var act = () => TradingVectorIdentity.EnsureUnique(vectors);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unique*Asset, Direction, TimeFrame, TradingLogic*");
    }

    [Fact]
    public void EnsureUnique_AllowsDifferentTimeFrames()
    {
        var asset = Asset.FromUsdmExchangeSymbol("BTCUSDT");
        var instrument = new InstrumentId(1);
        var vectors = new[]
        {
            new TradingVector(
                TradingVectorId.New(),
                asset,
                instrument,
                TimeFrameCode.Day1,
                Direction.Long,
                "Sma200Sma5",
                new Dictionary<string, string>()),
            new TradingVector(
                TradingVectorId.New(),
                asset,
                instrument,
                TimeFrameCode.Hour4,
                Direction.Long,
                "Sma200Sma5",
                new Dictionary<string, string>())
        };

        var act = () => TradingVectorIdentity.EnsureUnique(vectors);

        act.Should().NotThrow();
    }
}
