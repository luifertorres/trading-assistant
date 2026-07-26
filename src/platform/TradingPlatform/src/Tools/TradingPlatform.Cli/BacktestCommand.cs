using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Research.Application;
using Research.Domain;
using Research.Infrastructure;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli;

internal static class BacktestCommand
{
    private const string Venue = "binance";
    private const string Market = "usdm";
    private const string ContractType = "perpetual";

    public static async Task<BacktestCommandOutcome> RunAsync(
        BacktestArgs args,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedTradingLogic(args.TradingLogic))
        {
            Console.Error.WriteLine($"Unsupported trading logic '{args.TradingLogic}'.");
            return new BacktestCommandOutcome(1, null);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(args.ResearchDatabasePath)!);
        if (!string.IsNullOrWhiteSpace(args.VerdictDirectory))
            Directory.CreateDirectory(args.VerdictDirectory);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        }));
        services.AddMarketDataSqlite(args.MarketDatabasePath);
        services.AddResearchInfrastructure(args.ResearchDatabasePath);

        await using var provider = services.BuildServiceProvider();
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("backtest");
        var registry = provider.GetRequiredService<IInstrumentRegistry>();
        var runner = provider.GetRequiredService<IBacktestRunner>();

        var instrument = await registry.GetByExchangeSymbolAsync(
            Venue, Market, ContractType, args.Symbol, cancellationToken).ConfigureAwait(false);
        if (instrument is null)
        {
            Console.Error.WriteLine(
                $"Instrument '{args.Symbol}' not found in registry at {args.MarketDatabasePath}. " +
                "Run backfill first to populate instruments and candles.");
            return new BacktestCommandOutcome(1, null);
        }

        var timeFrame = TimeFrameCode.Parse(args.TimeFrame);
        var series = new SeriesDescriptor(instrument.Id, timeFrame);
        var cfg = new SimulationConfiguration(args.InitialCapital, args.FeeBpsPerSide, args.VectorRiskFraction);
        var asset = Asset.FromUsdmExchangeSymbol(args.Symbol);
        var vector = BuildVector(args, asset, instrument.Id, timeFrame);

        var candleReader = provider.GetRequiredService<ICandleSeriesReader>();
        var bars = await candleReader.ReadAsync(series, args.From, args.To, cancellationToken).ConfigureAwait(false);
        if (bars.Count == 0)
        {
            Console.Error.WriteLine(
                $"No {timeFrame.Value} bars for {args.Symbol} in the requested range. Run backfill or widen --from/--to.");
            return new BacktestCommandOutcome(1, null);
        }

        log.LogInformation(
            "Running backtest: {Asset} {Direction} {TimeFrame} {TradingLogic} capital {Capital}",
            asset.Value,
            args.Direction,
            timeFrame.Value,
            args.TradingLogic,
            args.InitialCapital);

        var result = await runner.RunAsync(
            new BacktestRequest(vector, cfg, args.From, args.To),
            cancellationToken).ConfigureAwait(false);

        log.LogInformation(
            "Run {RunId}: trades {Trades}, final equity {FinalEquity:F2}, max drawdown {MaxDrawdown:P2}",
            result.RunId,
            result.Trades.Count,
            result.FinalEquity,
            result.MaxDrawdownFraction);

        if (args.Save)
        {
            var runRepo = provider.GetRequiredService<ISimulationRunRepository>();
            await runRepo.SaveAsync(result, cancellationToken).ConfigureAwait(false);
            log.LogInformation("Saved run to research database: {Path}", args.ResearchDatabasePath);
        }

        if (!string.IsNullOrWhiteSpace(args.VerdictDirectory))
        {
            var verdict = BacktestVerdictEvaluator.Evaluate(
                args.Symbol,
                args.TradingLogic,
                timeFrame.Value,
                result,
                args.Direction,
                asset.Value);
            var store = new JsonBacktestVerdictStore(args.VerdictDirectory);
            await store.SaveAsync(verdict, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"VERDICT: {(verdict.Pass ? "PASS" : "FAIL")} — {verdict.FailReason ?? "ok"}");
        }

        return new BacktestCommandOutcome(0, result);
    }

    private static bool IsSupportedTradingLogic(string logic) =>
        logic.Equals("FixedWindow", StringComparison.OrdinalIgnoreCase) ||
        logic.Equals("Rsi5Extreme", StringComparison.OrdinalIgnoreCase) ||
        logic.Equals("Sma200Sma5", StringComparison.OrdinalIgnoreCase);

    private static TradingVector BuildVector(
        BacktestArgs args,
        Asset asset,
        InstrumentId instrumentId,
        TimeFrameCode timeFrame)
    {
        if (args.TradingLogic.Equals("Rsi5Extreme", StringComparison.OrdinalIgnoreCase))
        {
            return new TradingVector(
                TradingVectorId.New(),
                asset,
                instrumentId,
                timeFrame,
                args.Direction,
                "Rsi5Extreme",
                new Dictionary<string, string>
                {
                    ["takeProfitPct"] = args.TakeProfitPct.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["rsiExit"] = args.RsiExit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (args.TradingLogic.Equals("Sma200Sma5", StringComparison.OrdinalIgnoreCase))
        {
            return new TradingVector(
                TradingVectorId.New(),
                asset,
                instrumentId,
                timeFrame,
                args.Direction,
                "Sma200Sma5",
                new Dictionary<string, string>());
        }

        return new TradingVector(
            TradingVectorId.New(),
            asset,
            instrumentId,
            timeFrame,
            args.Direction,
            "FixedWindow",
            new Dictionary<string, string>
            {
                ["enterBar"] = args.EnterBar.ToString(),
                ["exitBar"] = args.ExitBar.ToString()
            });
    }
}
