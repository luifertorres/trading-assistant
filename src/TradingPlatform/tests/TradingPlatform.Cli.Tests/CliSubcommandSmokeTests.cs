using FluentAssertions;

namespace TradingPlatform.Cli.Tests;

/// <summary>Lightweight checks that existing CLI subcommand parsers were not regressed by backtest work.</summary>
public sealed class CliSubcommandSmokeTests
{
    [Fact]
    public void Backfill1dArgs_Parse_StillRequiresDataRoot()
    {
        var act = () => Backfill1dArgs.Parse(["backfill-1d", "--market-db", "market.sqlite"]);
        act.Should().Throw<ArgumentException>().WithMessage("*data-root*");
    }

    [Fact]
    public void Backfill1dArgs_Parse_AcceptsRequiredFlags()
    {
        var args = Backfill1dArgs.Parse(
        [
            "backfill-1d",
            "--market-db", "market.sqlite",
            "--data-root", ".trading-platform-data"
        ]);

        args.MarketDatabasePath.Should().Be("market.sqlite");
        args.DataRoot.Should().Be(".trading-platform-data");
        args.WriteSnapshot.Should().BeFalse();
    }

    [Fact]
    public void Backfill1dArgs_Parse_StillSupportsSnapshotFlag()
    {
        var args = Backfill1dArgs.Parse(
        [
            "backfill-1d",
            "--market-db", "market.sqlite",
            "--data-root", ".trading-platform-data",
            "--snapshot"
        ]);

        args.WriteSnapshot.Should().BeTrue();
    }
}
