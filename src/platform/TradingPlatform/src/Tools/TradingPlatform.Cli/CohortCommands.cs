using System.Globalization;
using System.Text.Json;
using Analytics.Application;
using Analytics.Infrastructure;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application;
using Portfolio.Infrastructure;
using Research.Application;
using Research.Domain;
using Research.Infrastructure;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli;

internal static class CohortBacktestCommand
{
    public static async Task<int> RunAsync(CohortBacktestArgs args, CancellationToken cancellationToken = default)
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
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("cohort-backtest");

        var cfg = new SimulationConfiguration(args.InitialCapital, args.FeeBpsPerSide, args.VectorRiskFraction);
        var exitCode = 0;

        Console.WriteLine("| Symbol | Trades | Return | MaxDD | PF | Verdict |");
        Console.WriteLine("|--------|--------|--------|-------|-----|---------|");

        foreach (var symbol in args.Symbols)
        {
            var instrument = await registry
                .GetByExchangeSymbolAsync("binance", "usdm", "perpetual", symbol, cancellationToken)
                .ConfigureAwait(false);
            if (instrument is null)
            {
                Console.WriteLine($"| {symbol} | - | - | - | - | FAIL (no instrument) |");
                exitCode = 1;
                continue;
            }

            var asset = Asset.FromUsdmExchangeSymbol(symbol);
            var vector = new TradingVectorSpec(
                TradingVectorId.New(),
                asset,
                instrument.Id,
                TimeFrameCode.Hour4,
                Direction.Long,
                "Rsi5Extreme",
                new Dictionary<string, string> { ["takeProfitPct"] = "0.08", ["rsiExit"] = "70" });

            var result = await runner.RunAsync(
                new BacktestRequest(vector, cfg, args.From, args.To),
                cancellationToken).ConfigureAwait(false);

            await runRepo.SaveAsync(result, cancellationToken).ConfigureAwait(false);
            var verdict = BacktestVerdictEvaluator.Evaluate(symbol, "Rsi5Extreme", "4H", result);
            await verdictStore.SaveAsync(verdict, cancellationToken).ConfigureAwait(false);

            var label = verdict.Pass ? "PASS" : "FAIL";
            if (!verdict.Pass)
                exitCode = 1;

            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2:P2} | {3:P2} | {4:F2} | {5} |",
                    symbol,
                    verdict.TradeCount,
                    verdict.TotalReturnFraction,
                    verdict.MaxDrawdownFraction,
                    verdict.ProfitFactor,
                    label));

            log.LogInformation("{Symbol} verdict {Label}: {Reason}", symbol, label, verdict.FailReason ?? "ok");
        }

        return exitCode;
    }
}

internal static class CohortComposeCommand
{
    public static async Task<int> RunAsync(CohortComposeArgs args, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(args.VerdictDirectory))
        {
            Console.Error.WriteLine("Verdict directory not found. Run cohort-backtest first.");
            return 1;
        }

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole());
        services.AddResearchInfrastructure(args.ResearchDatabasePath);
        services.AddAnalyticsInfrastructure();
        services.AddPortfolioInfrastructure(args.PortfolioDirectory);

        await using var provider = services.BuildServiceProvider();
        var runRepo = provider.GetRequiredService<ISimulationRunRepository>();
        var analytics = provider.GetRequiredService<IRunAnalytics>();
        var composer = provider.GetRequiredService<IPortfolioComposer>();
        var filePortfolios = provider.GetRequiredService<FilePortfolioRepository>();

        var recent = await runRepo.ListRecentAsync(50, cancellationToken).ConfigureAwait(false);
        var runById = recent.ToDictionary(r => r.RunId);
        var passRuns = new List<SimulationRunResult>();

        foreach (var vf in Directory.GetFiles(args.VerdictDirectory, "*-4H-Rsi5Extreme.json"))
        {
            var json = await File.ReadAllTextAsync(vf, cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("Pass", out var passEl) || !passEl.GetBoolean())
                continue;
            if (!root.TryGetProperty("RunId", out var runIdEl))
                continue;
            if (!Guid.TryParse(runIdEl.GetString(), out var runId))
                continue;
            if (runById.TryGetValue(runId, out var run))
                passRuns.Add(run);
        }

        if (passRuns.Count == 0)
        {
            Console.Error.WriteLine("No PASS verdict runs found in research DB. Run cohort-backtest first.");
            return 1;
        }

        var ranked = analytics.RankByReturn(passRuns);
        var matrix = analytics.UnderwaterCorrelationMatrix(passRuns);
        var portfolio = composer.ComposeDrawdownUncorrelated(passRuns, args.MaxPairwiseCorrelation, args.PortfolioName);
        await filePortfolios.SaveAsync(portfolio, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("## Comparison (ranked by return)");
        Console.WriteLine("| RunId | Return | MaxDD | Trades |");
        foreach (var m in ranked)
        {
            Console.WriteLine($"| {m.RunId:N} | {m.TotalReturnFraction:P2} | {m.MaxDrawdownFraction:P2} | {m.TradeCount} |");
        }

        Console.WriteLine("\n## Drawdown correlation matrix");
        foreach (var kv in matrix.OrderBy(k => k.Key.RunIdA).ThenBy(k => k.Key.RunIdB))
        {
            Console.WriteLine($"{kv.Key.RunIdA:N} x {kv.Key.RunIdB:N} = {kv.Value:F3}");
        }

        Console.WriteLine($"\n## Selected portfolio: {portfolio.Name} ({portfolio.Members.Count} members)");
        foreach (var member in portfolio.Members)
            Console.WriteLine($"  vector {member.VectorId.Value} weight {member.Weight:P0}");

        var outPath = Path.Combine(args.PortfolioDirectory, $"{portfolio.Name}.json");
        Console.WriteLine($"Saved: {outPath}");
        return 0;
    }
}
