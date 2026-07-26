using System.Globalization;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Research.Application;
using Research.Domain;
using Research.Infrastructure;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli;

internal sealed record UniverseBacktestArgs(
    string MarketDatabasePath,
    string ResearchDatabasePath,
    string VerdictDirectory,
    IReadOnlyList<string>? SymbolFilter,
    decimal InitialCapital,
    decimal FeeBpsPerSide,
    decimal VectorRiskFraction,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public const string Usage =
        "universe-backtest --market-db <path> [--research-db <path>] [--verdict-dir <path>] " +
        "[--symbols BTCUSDT,ETHUSDT,...] [--vector-risk 0.02] [--from iso] [--to iso]";

    public static UniverseBacktestArgs Parse(string[] args)
    {
        string? marketDb = null;
        string? researchDb = null;
        string? verdictDir = null;
        IReadOnlyList<string>? symbols = null;
        var initialCapital = 10_000m;
        var feeBps = 4m;
        var vectorRisk = 0.02m;
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;

        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            string TakeValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value after {a}");
                return args[++i];
            }

            if (a.Equals("--market-db", StringComparison.OrdinalIgnoreCase))
                marketDb = TakeValue();
            else if (a.Equals("--research-db", StringComparison.OrdinalIgnoreCase))
                researchDb = TakeValue();
            else if (a.Equals("--verdict-dir", StringComparison.OrdinalIgnoreCase))
                verdictDir = TakeValue();
            else if (a.Equals("--symbols", StringComparison.OrdinalIgnoreCase))
                symbols = TakeValue().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            else if (a.Equals("--from", StringComparison.OrdinalIgnoreCase))
                from = DateTimeOffset.Parse(TakeValue(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            else if (a.Equals("--to", StringComparison.OrdinalIgnoreCase))
                to = DateTimeOffset.Parse(TakeValue(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            else if (a.Equals("--initial-capital", StringComparison.OrdinalIgnoreCase))
                initialCapital = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--fee-bps", StringComparison.OrdinalIgnoreCase))
                feeBps = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--vector-risk", StringComparison.OrdinalIgnoreCase)
                     || a.Equals("--position-fraction", StringComparison.OrdinalIgnoreCase))
                vectorRisk = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else
                throw new ArgumentException($"Unknown argument: {a}. {Usage}");
        }

        if (string.IsNullOrWhiteSpace(marketDb))
            throw new ArgumentException($"--market-db is required. {Usage}");

        researchDb ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "research.sqlite");
        verdictDir ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "verdicts");

        return new UniverseBacktestArgs(
            Path.GetFullPath(marketDb),
            Path.GetFullPath(researchDb),
            Path.GetFullPath(verdictDir),
            symbols,
            initialCapital,
            feeBps,
            vectorRisk,
            from,
            to);
    }
}

internal static class UniverseBacktestCommand
{
    private const string TradingLogic = "Sma200Sma5";
    private static readonly TimeFrameCode TimeFrame = TimeFrameCode.Day1;

    public static async Task<int> RunAsync(UniverseBacktestArgs args, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(args.VerdictDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(args.ResearchDatabasePath)!);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
        services.AddMarketDataSqlite(args.MarketDatabasePath);
        services.AddResearchInfrastructure(args.ResearchDatabasePath);

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IInstrumentRegistry>();
        var runner = provider.GetRequiredService<IBacktestRunner>();
        var runRepo = provider.GetRequiredService<ISimulationRunRepository>();
        var verdictStore = new JsonBacktestVerdictStore(args.VerdictDirectory);
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("universe-backtest");

        var allInstruments = await registry.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var eligible = UsdmTradingUniverse.FilterEligible(allInstruments);
        if (args.SymbolFilter is { Count: > 0 } filter)
        {
            var set = filter.ToHashSet(StringComparer.Ordinal);
            eligible = eligible.Where(i => set.Contains(i.ExchangeSymbol)).ToList();
        }

        var vectors = new List<TradingVector>(eligible.Count * 2);
        foreach (var instrument in eligible)
        {
            var asset = Asset.FromUsdmExchangeSymbol(instrument.ExchangeSymbol);
            foreach (var direction in new[] { Direction.Long, Direction.Short })
            {
                vectors.Add(new TradingVector(
                    TradingVectorId.New(),
                    asset,
                    instrument.Id,
                    TimeFrame,
                    direction,
                    TradingLogic,
                    new Dictionary<string, string>()));
            }
        }

        TradingVectorIdentity.EnsureUnique(vectors);

        var cfg = new SimulationConfiguration(args.InitialCapital, args.FeeBpsPerSide, args.VectorRiskFraction);
        var exitCode = 0;
        var passCount = 0;
        var failCount = 0;

        Console.WriteLine("| Asset | Direction | Trades | Return | MaxDD | PF | Verdict |");
        Console.WriteLine("|-------|-----------|--------|--------|-------|-----|---------|");

        foreach (var vector in vectors)
        {
            var result = await runner.RunAsync(
                new BacktestRequest(vector, cfg, args.From, args.To),
                cancellationToken).ConfigureAwait(false);

            await runRepo.SaveAsync(result, cancellationToken).ConfigureAwait(false);
            var symbol = vector.Asset.Value.Split(':')[^1];
            var verdict = BacktestVerdictEvaluator.Evaluate(
                symbol,
                vector.TradingLogic,
                TimeFrame.Value,
                result,
                vector.Direction,
                vector.Asset.Value);
            await verdictStore.SaveAsync(verdict, cancellationToken).ConfigureAwait(false);

            var label = verdict.Pass ? "PASS" : "FAIL";
            if (verdict.Pass)
                passCount++;
            else
            {
                failCount++;
                exitCode = 1;
            }

            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2} | {3:P2} | {4:P2} | {5:F2} | {6} |",
                    vector.Asset.Value,
                    vector.Direction,
                    verdict.TradeCount,
                    verdict.TotalReturnFraction,
                    verdict.MaxDrawdownFraction,
                    verdict.ProfitFactor,
                    label));

            log.LogInformation(
                "{Asset} {Direction} verdict {Label}: {Reason}",
                vector.Asset.Value,
                vector.Direction,
                label,
                verdict.FailReason ?? "ok");
        }

        Console.WriteLine($"Universe complete: {vectors.Count} vectors, {passCount} PASS, {failCount} FAIL.");
        return exitCode;
    }
}
