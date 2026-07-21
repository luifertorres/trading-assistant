using FluentAssertions;
using TradingPlatform.Cli;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli.Tests;

public sealed class BacktestArgsTests
{
    [Fact]
    public void Parse_AppliesDefaults()
    {
        var args = BacktestArgs.Parse(["backtest", "--market-db", "market.sqlite"]);

        args.Symbol.Should().Be("BTCUSDT");
        args.TradingLogic.Should().Be("FixedWindow");
        args.Direction.Should().Be(Direction.Long);
        args.EnterBar.Should().Be(5);
        args.ExitBar.Should().Be(15);
        args.InitialCapital.Should().Be(10_000m);
        args.FeeBpsPerSide.Should().Be(4m);
        args.VectorRiskFraction.Should().Be(0.1m);
        args.Save.Should().BeFalse();
    }

    [Fact]
    public void Parse_AppliesCustomCapitalAndFees()
    {
        var args = BacktestArgs.Parse(
        [
            "backtest",
            "--market-db", "market.sqlite",
            "--initial-capital", "25000",
            "--fee-bps", "8",
            "--vector-risk", "0.25"
        ]);

        args.InitialCapital.Should().Be(25_000m);
        args.FeeBpsPerSide.Should().Be(8m);
        args.VectorRiskFraction.Should().Be(0.25m);
    }

    [Fact]
    public void Parse_AcceptsSma200Sma5ShortAndVectorRisk()
    {
        var args = BacktestArgs.Parse(
        [
            "backtest",
            "--market-db", "market.sqlite",
            "--trading-logic", "Sma200Sma5",
            "--direction", "Short",
            "--vector-risk", "0.02"
        ]);

        args.TradingLogic.Should().Be("Sma200Sma5");
        args.Direction.Should().Be(Direction.Short);
        args.VectorRiskFraction.Should().Be(0.02m);
    }

    [Fact]
    public void Parse_ParsesUtcFromAndTo()
    {
        var args = BacktestArgs.Parse(
        [
            "backtest",
            "--market-db", "market.sqlite",
            "--from", "2024-01-01T00:00:00Z",
            "--to", "2024-06-01T00:00:00Z"
        ]);

        args.From.Should().Be(DateTimeOffset.Parse("2024-01-01T00:00:00Z"));
        args.To.Should().Be(DateTimeOffset.Parse("2024-06-01T00:00:00Z"));
    }

    [Fact]
    public void Parse_RejectsUnknownFlag()
    {
        var act = () => BacktestArgs.Parse(["backtest", "--market-db", "m.sqlite", "--unknown", "x"]);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown argument*");
    }

    [Fact]
    public void Parse_RequiresMarketDb()
    {
        var act = () => BacktestArgs.Parse(["backtest", "--symbol", "ETHUSDT"]);
        act.Should().Throw<ArgumentException>().WithMessage("*--market-db is required*");
    }
}
