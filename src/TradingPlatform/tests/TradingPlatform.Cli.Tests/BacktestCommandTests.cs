using FluentAssertions;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Research.Application;
using Research.Infrastructure;
using TradingPlatform.Cli;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli.Tests;

public sealed class BacktestCommandTests
{
    [Fact]
    public async Task RunAsync_WithDay1Bars_ProducesTrades()
    {
        var marketDb = NewDbPath();
        var researchDb = NewDbPath();
        await SeedBtcUsdtDay1BarsAsync(marketDb, barCount: 30);

        var args = new BacktestArgs(
            marketDb,
            researchDb,
            "BTCUSDT",
            "FixedWindow",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 10_000m,
            FeeBpsPerSide: 4m,
            PositionNotionalFraction: 0.1m,
            From: null,
            To: null,
            Save: false);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(0);
        outcome.Result.Should().NotBeNull();
        outcome.Result!.Trades.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithSave_PersistsRunToResearchDb()
    {
        var marketDb = NewDbPath();
        var researchDb = NewDbPath();
        await SeedBtcUsdtDay1BarsAsync(marketDb, barCount: 30);

        var args = new BacktestArgs(
            marketDb,
            researchDb,
            "BTCUSDT",
            "FixedWindow",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 10_000m,
            FeeBpsPerSide: 4m,
            PositionNotionalFraction: 0.1m,
            From: null,
            To: null,
            Save: true);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(0);
        outcome.Result.Should().NotBeNull();

        var services = new ServiceCollection();
        services.AddResearchInfrastructure(researchDb);
        await using var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<ISimulationRunRepository>();
        var saved = await repo.ListRecentAsync(1);

        saved.Should().ContainSingle();
        saved[0].RunId.Should().Be(outcome.Result!.RunId);
        saved[0].Trades.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_CustomCapital_ReflectedInSimulationResult()
    {
        var marketDb = NewDbPath();
        var researchDb = NewDbPath();
        await SeedBtcUsdtDay1BarsAsync(marketDb, barCount: 30);

        var args = new BacktestArgs(
            marketDb,
            researchDb,
            "BTCUSDT",
            "FixedWindow",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 25_000m,
            FeeBpsPerSide: 8m,
            PositionNotionalFraction: 0.25m,
            From: null,
            To: null,
            Save: false);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(0);
        outcome.Result!.Configuration.InitialCapital.Should().Be(25_000m);
        outcome.Result.Configuration.FeeBpsPerSide.Should().Be(8m);
        outcome.Result.Configuration.PositionNotionalFraction.Should().Be(0.25m);
    }

    [Fact]
    public async Task RunAsync_MissingInstrument_ReturnsNonZero()
    {
        var marketDb = NewDbPath();
        var researchDb = NewDbPath();
        BuildMarketProvider(marketDb);

        var args = new BacktestArgs(
            marketDb,
            researchDb,
            "BTCUSDT",
            "FixedWindow",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 10_000m,
            FeeBpsPerSide: 4m,
            PositionNotionalFraction: 0.1m,
            From: null,
            To: null,
            Save: false);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(1);
        outcome.Result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_EmptyBarSeries_ReturnsNonZero()
    {
        var marketDb = NewDbPath();
        var researchDb = NewDbPath();
        using (var provider = BuildMarketProvider(marketDb))
        {
            var registry = provider.GetRequiredService<IInstrumentRegistry>();
            await registry.UpsertAsync(SampleUpsert("BTCUSDT"));
        }

        var args = new BacktestArgs(
            marketDb,
            researchDb,
            "BTCUSDT",
            "FixedWindow",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 10_000m,
            FeeBpsPerSide: 4m,
            PositionNotionalFraction: 0.1m,
            From: null,
            To: null,
            Save: false);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(1);
        outcome.Result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_UnsupportedStrategy_ReturnsNonZero()
    {
        var args = new BacktestArgs(
            NewDbPath(),
            NewDbPath(),
            "BTCUSDT",
            "RsiCross",
            EnterBar: 3,
            ExitBar: 12,
            InitialCapital: 10_000m,
            FeeBpsPerSide: 4m,
            PositionNotionalFraction: 0.1m,
            From: null,
            To: null,
            Save: false);

        var outcome = await BacktestCommand.RunAsync(args);

        outcome.ExitCode.Should().Be(1);
        outcome.Result.Should().BeNull();
    }

    private static async Task SeedBtcUsdtDay1BarsAsync(string marketDb, int barCount)
    {
        using var provider = BuildMarketProvider(marketDb);
        var registry = provider.GetRequiredService<IInstrumentRegistry>();
        var writer = provider.GetRequiredService<ICandleSeriesWriter>();
        var instrumentId = await registry.UpsertAsync(SampleUpsert("BTCUSDT"));
        var series = new SeriesDescriptor(instrumentId, TimeFrameCode.Day1);
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var bars = new List<OhlcBar>(barCount);
        var open = start;
        for (var i = 0; i < barCount; i++)
        {
            bars.Add(new OhlcBar(open, open.AddDays(1), 50_000m, 50_100m, 49_900m, 50_050m, 100m));
            open = open.AddDays(1);
        }

        await writer.UpsertAsync(series, bars);
    }

    private static ServiceProvider BuildMarketProvider(string dbPath)
    {
        var services = new ServiceCollection();
        services.AddMarketDataSqlite(dbPath);
        return services.BuildServiceProvider();
    }

    private static InstrumentUpsert SampleUpsert(string exchangeSymbol) =>
        new(
            "binance",
            "usdm",
            "perpetual",
            exchangeSymbol,
            exchangeSymbol[..^4],
            "USDT",
            exchangeSymbol,
            2,
            3,
            "[]",
            "TRADING",
            DateTimeOffset.UtcNow);

    private static string NewDbPath() =>
        Path.Combine(Path.GetTempPath(), $"tp-cli-{Guid.NewGuid():N}.sqlite");
}
