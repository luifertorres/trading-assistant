using TradingPlatform.Cli;
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

try
{
    switch (cmd.ToLowerInvariant())
    {
        case "backfill-1d":
            await RunBackfillAsync(args, TimeFrameCode.Day1, null, "backfill-1d-checkpoint.json").ConfigureAwait(false);
            return;
        case "backfill-4h":
        {
            var symbols = ParseSymbolsArg(args) ?? CohortSymbols.Default;
            await RunBackfillAsync(args, TimeFrameCode.Hour4, symbols, "backfill-4h-checkpoint.json").ConfigureAwait(false);
            return;
        }
        case "backtest":
        {
            var outcome = await BacktestCommand.RunAsync(BacktestArgs.Parse(args)).ConfigureAwait(false);
            Environment.Exit(outcome.ExitCode);
            return;
        }
        case "cohort-backtest":
            Environment.Exit(await CohortBacktestCommand.RunAsync(CohortBacktestArgs.Parse(args)).ConfigureAwait(false));
            return;
        case "cohort-compose":
            Environment.Exit(await CohortComposeCommand.RunAsync(CohortComposeArgs.Parse(args)).ConfigureAwait(false));
            return;
        case "fire-test-order":
            Environment.Exit(await FireTestOrderCommand.RunAsync(FireTestOrderArgs.Parse(args)).ConfigureAwait(false));
            return;
        case "universe-backtest":
            Environment.Exit(await UniverseBacktestCommand.RunAsync(UniverseBacktestArgs.Parse(args)).ConfigureAwait(false));
            return;
        case "demo":
            await RunDemoAsync().ConfigureAwait(false);
            return;
        default:
            PrintUsage();
            return;
    }
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

static void PrintUsage()
{
    Console.WriteLine(
        "Usage: TradingPlatform.Cli <command>\n" +
        "  demo\n" +
        "  backfill-1d --market-db <path> --data-root <path> [--checkpoint <path>] [--snapshot]\n" +
        "  backfill-4h --market-db <path> --data-root <path> [--symbols A,B,C] [--checkpoint <path>]\n" +
        $"  {BacktestArgs.Usage}\n" +
        $"  {CohortBacktestArgs.Usage}\n" +
        $"  {CohortComposeArgs.Usage}\n" +
        $"  {UniverseBacktestArgs.Usage}\n" +
        $"  {FireTestOrderArgs.Usage}");
}

static async Task RunBackfillAsync(string[] args, TimeFrameCode timeFrame, IReadOnlyList<string>? symbolFilter, string defaultCheckpointName)
{
    var parsed = BackfillArgs.Parse(args);
    var dataRoot = Path.GetFullPath(parsed.DataRoot);
    Directory.CreateDirectory(dataRoot);
    var marketDb = Path.GetFullPath(parsed.MarketDatabasePath);
    var checkpointPath = string.IsNullOrWhiteSpace(parsed.CheckpointPath)
        ? Path.Combine(dataRoot, defaultCheckpointName)
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
    var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("backfill");
    var runner = provider.GetRequiredService<UsdmBackfillOrchestrator>();

    log.LogInformation("Market DB: {Path} TimeFrame: {Tf}", marketDb, timeFrame.Value);
    await runner.RunAsync(
            new UsdmBackfillRunOptions(marketDb, dataRoot, checkpointPath, parsed.WriteSnapshot, timeFrame, symbolFilter),
            CancellationToken.None)
        .ConfigureAwait(false);
}

static IReadOnlyList<string>? ParseSymbolsArg(string[] args)
{
    for (var i = 1; i < args.Length - 1; i++)
    {
        if (args[i].Equals("--symbols", StringComparison.OrdinalIgnoreCase))
            return args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    return null;
}

static async Task RunDemoAsync()
{
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

    var writer = provider.GetRequiredService<ICandleSeriesWriter>();
    var registry = provider.GetRequiredService<IInstrumentRegistry>();
    var runner = provider.GetRequiredService<IBacktestRunner>();
    var runRepo = provider.GetRequiredService<ISimulationRunRepository>();
    var analytics = provider.GetRequiredService<IRunAnalytics>();
    var composer = provider.GetRequiredService<IPortfolioComposer>();
    var filePortfolios = provider.GetRequiredService<FilePortfolioRepository>();
    var router = provider.GetRequiredService<PortfolioExecutionRouter>();

    var instrumentId = await registry.UpsertAsync(new InstrumentUpsert(
        "binance", "usdm", "perpetual", "BTCUSDT",
        "BTC", "USDT", "BTCUSDT", 2, 3, "[]", "TRADING", DateTimeOffset.UtcNow)).ConfigureAwait(false);
    var bars = SyntheticBars(count: 40, start: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    await writer.UpsertAsync(new SeriesDescriptor(instrumentId, TimeFrameCode.Min1), bars).ConfigureAwait(false);

    var cfg = new SimulationConfiguration(InitialCapital: 10_000m, FeeBpsPerSide: 5m, VectorRiskFraction: 0.1m);
    var asset = Asset.FromUsdmExchangeSymbol("BTCUSDT");
    var v1 = new TradingVector(
        TradingVectorId.New(), asset, instrumentId, TimeFrameCode.Min1, Direction.Long, "FixedWindow",
        new Dictionary<string, string> { ["enterBar"] = "3", ["exitBar"] = "12" });
    var v2 = new TradingVector(
        TradingVectorId.New(), asset, instrumentId, TimeFrameCode.Min1, Direction.Long, "FixedWindow",
        new Dictionary<string, string> { ["enterBar"] = "5", ["exitBar"] = "18" });

    var r1 = await runner.RunAsync(new BacktestRequest(v1, cfg, null, null)).ConfigureAwait(false);
    var r2 = await runner.RunAsync(new BacktestRequest(v2, cfg, null, null)).ConfigureAwait(false);
    await runRepo.SaveAsync(r1).ConfigureAwait(false);
    await runRepo.SaveAsync(r2).ConfigureAwait(false);

    var portfolio = composer.ComposeDrawdownUncorrelated([r1, r2], maxPairwiseCorrelation: 0.99, "demo-portfolio");
    await filePortfolios.SaveAsync(portfolio).ConfigureAwait(false);
    await router.ExecuteOneShotAsync(portfolio, new Dictionary<TradingVectorId, TradingVector> { [v1.Id] = v1, [v2.Id] = v2 }, bars, v1.Id).ConfigureAwait(false);
    log.LogInformation("Demo complete: portfolio {Name} members {N}", portfolio.Name, portfolio.Members.Count);
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

internal sealed record BackfillArgs(string MarketDatabasePath, string DataRoot, string? CheckpointPath, bool WriteSnapshot)
{
    public static BackfillArgs Parse(string[] args)
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

            if (a.Equals("--symbols", StringComparison.OrdinalIgnoreCase))
            {
                i++;
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
                "Usage: backfill-1d|backfill-4h --market-db <path> --data-root <path> [--checkpoint <path>] [--symbols A,B] [--snapshot]");
        }

        return new BackfillArgs(marketDb, dataRoot, checkpoint, snapshot);
    }
}
