using FluentAssertions;
using TradingPlatform.Cli;

namespace TradingPlatform.Cli.Tests;

public sealed class UniverseBacktestArgsTests
{
    [Fact]
    public void Parse_AppliesDefaults()
    {
        var args = UniverseBacktestArgs.Parse(["universe-backtest", "--market-db", "market.sqlite"]);

        args.MarketDatabasePath.Should().EndWith("market.sqlite");
        args.VectorRiskFraction.Should().Be(0.02m);
        args.SymbolFilter.Should().BeNull();
    }

    [Fact]
    public void Parse_AcceptsSymbolsAndVectorRisk()
    {
        var args = UniverseBacktestArgs.Parse(
        [
            "universe-backtest",
            "--market-db", "market.sqlite",
            "--symbols", "BTCUSDT,ETHUSDT",
            "--vector-risk", "0.03"
        ]);

        args.SymbolFilter.Should().Equal("BTCUSDT", "ETHUSDT");
        args.VectorRiskFraction.Should().Be(0.03m);
    }

    [Fact]
    public void Usage_ContainsUniverseBacktestCommand()
    {
        UniverseBacktestArgs.Usage.Should().Contain("universe-backtest");
        UniverseBacktestArgs.Usage.Should().Contain("--vector-risk");
    }
}
