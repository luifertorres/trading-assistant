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
        args.TradingLogic.Should().Be("Rsi5ExtremeSma200");
        args.InitialCapital.Should().Be(100m);
        args.FeeBpsPerSide.Should().Be(5m);
        args.VectorRiskFraction.Should().Be(0.05m);
        args.RsiExit.Should().Be(70m);
        args.SymbolFilter.Should().BeNull();
        args.HtmlReportPath.Should().NotBeNullOrWhiteSpace();
        args.HtmlReportPath.Should().Contain("universe-Rsi5ExtremeSma200");
        args.HtmlReportPath.Should().EndWith(".html");
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
        UniverseBacktestArgs.Usage.Should().Contain("--html-report");
    }
}
