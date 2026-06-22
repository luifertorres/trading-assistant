using FluentAssertions;

namespace TradingPlatform.Cli.Tests;

/// <summary>Lightweight checks that existing CLI subcommand parsers were not regressed.</summary>
public sealed class CliSubcommandSmokeTests
{
    [Fact]
    public void BackfillArgs_Parse_StillRequiresDataRoot()
    {
        var act = () => BackfillArgs.Parse(["backfill-1d", "--market-db", "market.sqlite"]);
        act.Should().Throw<ArgumentException>().WithMessage("*data-root*");
    }

    [Fact]
    public void BackfillArgs_Parse_AcceptsRequiredFlags()
    {
        var args = BackfillArgs.Parse(
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
    public void BackfillArgs_Parse_StillSupportsSnapshotFlag()
    {
        var args = BackfillArgs.Parse(
        [
            "backfill-1d",
            "--market-db", "market.sqlite",
            "--data-root", ".trading-platform-data",
            "--snapshot"
        ]);

        args.WriteSnapshot.Should().BeTrue();
    }
}
