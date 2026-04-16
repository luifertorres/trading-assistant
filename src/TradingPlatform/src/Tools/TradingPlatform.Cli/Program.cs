using Analytics.Application;
using Analytics.Infrastructure;
using Execution.Application;
using Execution.Infrastructure;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application;
using Portfolio.Domain;
using Portfolio.Infrastructure;
using Research.Application;
using Research.Domain;
using Research.Infrastructure;
using TradingPlatform.Kernel;

var cmd = args.Length > 0 ? args[0] : "demo";
if (cmd.Equals("backfill-1d", StringComparison.OrdinalIgnoreCase))
{
    await RunBackfill1dAsync(args).ConfigureAwait(false);
    return;
}

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));

var dataRoot = Path.Combine(Environment.CurrentDirectory, ".trading-platform-data");
Directory.CreateDirectory(dataRoot);
var marketDb = Path.Combine(dataRoot, "market.sqlite");
var researchDb = Path.Combine(dataRoot, "research.sqlite");
var portfolioDir = Path.Combine(dataRoot, "portfolios");

services.AddMarketDataSqlite(marketDb);
services.AddResearchInfrastructure(researchDb);
services.AddAnalyticsInfrastructure();
services.AddPortfolioInfrastructure(portfolioDir);
services.AddExecutionInfrastructure();

await using var provider = services.BuildServiceProvider();
var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Cli");

switch (cmd.ToLowerInvariant())
{
    case "demo":
        await RunDemoAsync(provider, log).ConfigureAwait(false);
        break;
    default:
        log.LogInformation("Usage: TradingPlatform.Cli [demo|backfill-1d] …");
        break;
}

static async Task RunBackfill1dAsync(string[] args)
{
    var parsed = Backfill1dArgs.Parse(args);
    var dataRoot = Path.GetFullPath(parsed.DataRoot);
    Directory.CreateDirectory(dataRoot);
    var marketDb = Path.GetFullPath(parsed.MarketDatabasePath);
    var checkpointPath = string.IsNullOrWhiteSpace(parsed.CheckpointPath)
        ? Path.Combine(dataRoot, "backfill-1d-checkpoint.json")
        : Path.GetFullPath(parsed.CheckpointPath);

    var services = new ServiceCollection();
    services.AddLogging(b => b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    }));
    services.AddMarketDataSqlite(marketDb);
    services.AddMarketDataBinanceUsdM1dBackfill(checkpointPath);

    await using var provider = services.BuildServiceProvider();
    var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("backfill-1d");
    var runner = provider.GetRequiredService<Usdm1dBackfillOrchestrator>();

    log.LogInformation("Market DB: {Path}", marketDb);
    log.LogInformation("Data root: {Path}", dataRoot);
    log.LogInformation("Checkpoint: {Path}", checkpointPath);

    await runner.RunAsync(
            new Usdm1dBackfillRunOptions(marketDb, dataRoot, checkpointPath, parsed.WriteSnapshot),
            CancellationToken.None)
        .ConfigureAwait(false);
}

static async Task RunDemoAsync(ServiceProvider provider, ILogger log)
{
    var writer = provider.GetRequiredService<ICandleSeriesWriter>();
    var runner = provider.GetRequiredService<IBacktestRunner>();
    var runRepo = provider.GetRequiredService<ISimulationRunRepository>();
    var analytics = provider.GetRequiredService<IRunAnalytics>();
    var composer = provider.GetRequiredService<IPortfolioComposer>();
    var filePortfolios = provider.GetRequiredService<FilePortfolioRepository>();
    var router = provider.GetRequiredService<PortfolioExecutionRouter>();
    var candles = provider.GetRequiredService<ICandleSeriesReader>();

    var series = new SeriesDescriptor("BTCUSDT", TimeFrameCode.Min1);
    var bars = SyntheticBars(count: 40, start: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    await writer.UpsertAsync(series, bars).ConfigureAwait(false);
    log.LogInformation("Seeded {Count} bars into per-series store.", bars.Count);

    var cfg = new SimulationConfiguration(InitialCapital: 10_000m, FeeBpsPerSide: 4m, PositionNotionalFraction: 0.1m);
    var v1 = new TradingVectorSpec(
        TradingVectorId.New(),
        "BTCUSDT",
        TimeFrameCode.Min1,
        PositionSide.Long,
        "FixedWindow",
        new Dictionary<string, string> { ["enterBar"] = "3", ["exitBar"] = "12" });
    var v2 = new TradingVectorSpec(
        TradingVectorId.New(),
        "BTCUSDT",
        TimeFrameCode.Min1,
        PositionSide.Long,
        "FixedWindow",
        new Dictionary<string, string> { ["enterBar"] = "5", ["exitBar"] = "18" });

    var r1 = await runner.RunAsync(new BacktestRequest(v1, cfg, null, null)).ConfigureAwait(false);
    var r2 = await runner.RunAsync(new BacktestRequest(v2, cfg, null, null)).ConfigureAwait(false);
    await runRepo.SaveAsync(r1).ConfigureAwait(false);
    await runRepo.SaveAsync(r2).ConfigureAwait(false);
    log.LogInformation("Backtests: run {A} final {Eq1:F2}; run {B} final {Eq2:F2}", r1.RunId, r1.FinalEquity, r2.RunId, r2.FinalEquity);

    var ranked = analytics.RankByReturn([r1, r2]);
    log.LogInformation("Ranked by return: {R}", string.Join(", ", ranked.Select(x => $"{x.RunId}:{x.TotalReturnFraction:P2}")));

    var portfolio = composer.ComposeDrawdownUncorrelated([r1, r2], maxPairwiseCorrelation: 0.99, "demo-portfolio");
    await filePortfolios.SaveAsync(portfolio).ConfigureAwait(false);
    log.LogInformation("Portfolio {Name} with {N} members.", portfolio.Name, portfolio.Members.Count);

    var vectorMap = new Dictionary<TradingVectorId, TradingVectorSpec> { [v1.Id] = v1, [v2.Id] = v2 };
    var active = portfolio.Members[0].VectorId;
    await router.ExecuteOneShotAsync(portfolio, vectorMap, bars, active).ConfigureAwait(false);
    log.LogInformation("Execution router (live sink stub) completed for vector {V}.", active.Value);
}

static IReadOnlyList<OhlcBar> SyntheticBars(int count, DateTimeOffset start)
{
    var list = new List<OhlcBar>(count);
    var rnd = new Random(42);
    var t = start;
    decimal p = 50_000m;
    for (var i = 0; i < count; i++)
    {
        var d = (decimal)(rnd.NextDouble() * 80 - 40);
        var o = p;
        var c = p + d;
        var h = Math.Max(o, c) + 5;
        var l = Math.Min(o, c) - 5;
        var close = t.AddMinutes(1);
        list.Add(new OhlcBar(t, close, o, h, l, c, 100));
        p = c;
        t = close;
    }

    return list;
}

internal sealed record Backfill1dArgs(string MarketDatabasePath, string DataRoot, string? CheckpointPath, bool WriteSnapshot)
{
    public static Backfill1dArgs Parse(string[] args)
    {
        string? marketDb = null;
        string? dataRoot = null;
        string? checkpoint = null;
        var snapshot = false;

        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a.Equals("--snapshot", StringComparison.OrdinalIgnoreCase))
            {
                snapshot = true;
                continue;
            }

            string? TakeValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value after {a}");
                return args[++i];
            }

            if (a.Equals("--market-db", StringComparison.OrdinalIgnoreCase))
                marketDb = TakeValue();
            else if (a.Equals("--data-root", StringComparison.OrdinalIgnoreCase))
                dataRoot = TakeValue();
            else if (a.Equals("--checkpoint", StringComparison.OrdinalIgnoreCase))
                checkpoint = TakeValue();
            else
                throw new ArgumentException($"Unknown argument: {a}");
        }

        if (string.IsNullOrWhiteSpace(marketDb) || string.IsNullOrWhiteSpace(dataRoot))
        {
            throw new ArgumentException(
                "Usage: TradingPlatform.Cli backfill-1d --market-db <path> --data-root <path> [--checkpoint <path>] [--snapshot]");
        }

        return new Backfill1dArgs(marketDb, dataRoot, checkpoint, snapshot);
    }
}
