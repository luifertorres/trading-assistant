using FluentAssertions;
using TradingPlatform.Cli;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli.Tests;

public sealed class UniverseBacktestHtmlReportTests
{
    [Fact]
    public void Render_IncludesSummaryAndRows()
    {
        var args = UniverseBacktestArgs.Parse(["universe-backtest", "--market-db", "market.sqlite"]);
        var rows = new[]
        {
            new UniverseBacktestRow("binance:usdm:BTCUSDT", "BTCUSDT", Direction.Long, 6, 0.0179m, 0.0104m, 20.24m, true, null),
            new UniverseBacktestRow("binance:usdm:BTCUSDT", "BTCUSDT", Direction.Short, 1, -0.0059m, 0.0128m, 0m, false, "trades 1 < 3"),
            new UniverseBacktestRow("binance:usdm:CHZUSDT", "CHZUSDT", Direction.Short, 6, 0.0422m, 0.004m, 999m, true, null)
        };

        var html = UniverseBacktestHtmlReport.Render(args, instrumentCount: 1, vectorCount: 2, rows);

        html.Should().Contain("Universe backtest");
        html.Should().Contain("Rsi5ExtremeSma200");
        html.Should().Contain("BTCUSDT");
        html.Should().Contain("PASS");
        html.Should().Contain("FAIL");
        html.Should().Contain("trades 1 &lt; 3");
        html.Should().Contain("data-value=\"999\"");
    }
}
