using Execution.Application;
using Execution.Infrastructure;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli;

internal static class FireTestOrderCommand
{
    public static async Task<int> RunAsync(FireTestOrderArgs args, CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
        services.AddMarketDataSqlite(args.MarketDatabasePath);
        services.Configure<LiveTradingOptions>(o =>
        {
            o.Armed = args.Arm;
            o.VerdictDirectory = args.VerdictDirectory;
        });
        services.AddExecutionInfrastructure(liveTrading: true);

        await using var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<ILiveOrderIntentSink>();
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("fire-test-order");

        if (!args.Arm)
        {
            log.LogWarning("Disarmed — no order sent. Re-run with --arm after PASS verdict exists.");
            return 2;
        }

        var verdictFile = Path.Combine(args.VerdictDirectory, $"{args.Symbol}-4H-Rsi5Extreme.json");
        if (!File.Exists(verdictFile))
        {
            log.LogError("No verdict file at {Path}. Run cohort-backtest first.", verdictFile);
            return 1;
        }

        var sl = 0m;
        await sink.OnIntentAsync(
            new OrderIntent(
                OrderIntentKind.OpenLong,
                0,
                $"fire-test:{args.Symbol}",
                StopLossPrice: sl > 0 ? sl : null),
            cancellationToken).ConfigureAwait(false);

        log.LogInformation("Fire-test order flow completed for {Symbol} (check Binance Desktop).", args.Symbol);
        return 0;
    }
}
