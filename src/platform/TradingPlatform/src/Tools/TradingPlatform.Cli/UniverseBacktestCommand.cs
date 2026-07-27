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
    string? HtmlReportPath,
    IReadOnlyList<string>? SymbolFilter,
    string TradingLogic,
    decimal RsiExit,
    decimal InitialCapital,
    decimal FeeBpsPerSide,
    decimal VectorRiskFraction,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public const string Usage =
        "universe-backtest --market-db <path> [--research-db <path>] [--verdict-dir <path>] " +
        "[--html-report <path>] [--trading-logic Rsi5ExtremeSma200|Sma200Sma5] [--symbols BTCUSDT,ETHUSDT,...] " +
        "[--initial-capital 100] [--vector-risk 0.05] [--fee-bps 5] [--rsi-exit 70] [--from iso] [--to iso]";

    public static UniverseBacktestArgs Parse(string[] args)
    {
        string? marketDb = null;
        string? researchDb = null;
        string? verdictDir = null;
        string? htmlReport = null;
        IReadOnlyList<string>? symbols = null;
        var tradingLogic = "Rsi5ExtremeSma200";
        var rsiExit = 70m;
        var initialCapital = 100m;
        var feeBps = 5m;
        var vectorRisk = 0.05m;
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
            else if (a.Equals("--html-report", StringComparison.OrdinalIgnoreCase))
                htmlReport = TakeValue();
            else if (a.Equals("--symbols", StringComparison.OrdinalIgnoreCase))
                symbols = TakeValue().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            else if (a.Equals("--strategy", StringComparison.OrdinalIgnoreCase)
                     || a.Equals("--trading-logic", StringComparison.OrdinalIgnoreCase))
                tradingLogic = TakeValue();
            else if (a.Equals("--rsi-exit", StringComparison.OrdinalIgnoreCase))
                rsiExit = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
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
        htmlReport ??= Path.Combine(
            Environment.CurrentDirectory,
            ".trading-platform-data",
            "reports",
            $"universe-{tradingLogic}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");

        return new UniverseBacktestArgs(
            Path.GetFullPath(marketDb),
            Path.GetFullPath(researchDb),
            Path.GetFullPath(verdictDir),
            Path.GetFullPath(htmlReport),
            symbols,
            tradingLogic,
            rsiExit,
            initialCapital,
            feeBps,
            vectorRisk,
            from,
            to);
    }
}

internal static class UniverseBacktestCommand
{
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

        var vectorParameters = BuildVectorParameters(args);

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
                    args.TradingLogic,
                    vectorParameters));
            }
        }

        TradingVectorIdentity.EnsureUnique(vectors);

        var cfg = new SimulationConfiguration(args.InitialCapital, args.FeeBpsPerSide, args.VectorRiskFraction);
        var exitCode = 0;
        var passCount = 0;
        var failCount = 0;
        var rows = new List<UniverseBacktestRow>(vectors.Count);

        Console.WriteLine($"Universe: {eligible.Count} instruments, {vectors.Count} vectors, logic {args.TradingLogic}, TF {TimeFrame.Value}");
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

            rows.Add(new UniverseBacktestRow(
                vector.Asset.Value,
                symbol,
                vector.Direction,
                verdict.TradeCount,
                verdict.TotalReturnFraction,
                verdict.MaxDrawdownFraction,
                verdict.ProfitFactor,
                verdict.Pass,
                verdict.FailReason));

            log.LogInformation(
                "{Asset} {Direction} verdict {Label}: {Reason}",
                vector.Asset.Value,
                vector.Direction,
                label,
                verdict.FailReason ?? "ok");
        }

        Console.WriteLine($"Universe complete: {vectors.Count} vectors, {passCount} PASS, {failCount} FAIL.");

        if (!string.IsNullOrWhiteSpace(args.HtmlReportPath))
        {
            var html = UniverseBacktestHtmlReport.Render(args, eligible.Count, vectors.Count, rows);
            await UniverseBacktestHtmlReport.WriteAsync(args.HtmlReportPath, html, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"HTML report: {args.HtmlReportPath}");
            log.LogInformation("Wrote HTML report to {Path}", args.HtmlReportPath);
        }

        return exitCode;
    }

    private static Dictionary<string, string> BuildVectorParameters(UniverseBacktestArgs args)
    {
        if (args.TradingLogic.Equals("Rsi5ExtremeSma200", StringComparison.OrdinalIgnoreCase)
            || args.TradingLogic.Equals("Rsi5Extreme", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, string>
            {
                ["rsiExit"] = args.RsiExit.ToString(CultureInfo.InvariantCulture)
            };
        }

        return new Dictionary<string, string>();
    }
}
