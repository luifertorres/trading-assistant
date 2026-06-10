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
        if (!args.StrategyKind.Equals("FixedWindow", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unsupported strategy kind '{args.StrategyKind}'. Only FixedWindow is supported.");
            return new BacktestCommandOutcome(1, null);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(args.ResearchDatabasePath)!);

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
        var candles = provider.GetRequiredService<ICandleSeriesReader>();
        var runner = provider.GetRequiredService<IBacktestRunner>();

        var instrument = await registry.GetByExchangeSymbolAsync(
            Venue, Market, ContractType, args.Symbol, cancellationToken).ConfigureAwait(false);
        if (instrument is null)
        {
            Console.Error.WriteLine(
                $"Instrument '{args.Symbol}' not found in registry at {args.MarketDatabasePath}. " +
                "Run 'backfill-1d' first to populate instruments and Day1 candles.");
            return new BacktestCommandOutcome(1, null);
        }

        var series = new SeriesDescriptor(instrument.Id, TimeFrameCode.Day1);
        var bars = await candles.ReadAsync(series, args.From, args.To, cancellationToken).ConfigureAwait(false);
        if (bars.Count == 0)
        {
            Console.Error.WriteLine(
                $"No Day1 bars for {args.Symbol} (instrument {instrument.Id.Value}) in the requested range. " +
                "Run 'backfill-1d' or widen --from/--to.");
            return new BacktestCommandOutcome(1, null);
        }

        var cfg = new SimulationConfiguration(args.InitialCapital, args.FeeBpsPerSide, args.PositionNotionalFraction);
        var vector = new TradingVectorSpec(
            TradingVectorId.New(),
            instrument.Id,
            TimeFrameCode.Day1,
            PositionSide.Long,
            "FixedWindow",
            new Dictionary<string, string>
            {
                ["enterBar"] = args.EnterBar.ToString(),
                ["exitBar"] = args.ExitBar.ToString()
            });

        log.LogInformation(
            "Running backtest: instrument {InstrumentId}, symbol {Symbol}, bars {BarCount}, range {From}..{To}",
            instrument.Id.Value,
            args.Symbol,
            bars.Count,
            args.From?.ToString("O") ?? "(all)",
            args.To?.ToString("O") ?? "(all)");

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

        return new BacktestCommandOutcome(0, result);
    }
}
