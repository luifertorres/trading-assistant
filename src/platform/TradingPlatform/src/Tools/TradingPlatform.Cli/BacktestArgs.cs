using System.Globalization;

namespace TradingPlatform.Cli;

internal sealed record BacktestArgs(
    string MarketDatabasePath,
    string ResearchDatabasePath,
    string? VerdictDirectory,
    string Symbol,
    string StrategyKind,
    string TimeFrame,
    int EnterBar,
    int ExitBar,
    decimal TakeProfitPct,
    decimal RsiExit,
    decimal InitialCapital,
    decimal FeeBpsPerSide,
    decimal PositionNotionalFraction,
    DateTimeOffset? From,
    DateTimeOffset? To,
    bool Save)
{
    public const string Usage =
        "Usage: TradingPlatform.Cli backtest --market-db <path> " +
        "[--research-db <path>] [--verdict-dir <path>] [--symbol <exchangeSymbol>] " +
        "[--timeframe 4H|1D] [--from <iso8601>] [--to <iso8601>] " +
        "[--strategy FixedWindow|Rsi5Extreme] [--enter-bar <int>] [--exit-bar <int>] " +
        "[--take-profit-pct <decimal>] [--rsi-exit <decimal>] " +
        "[--initial-capital <decimal>] [--fee-bps <decimal>] [--position-fraction <decimal>] [--save]";

    public static BacktestArgs Parse(string[] args)
    {
        string? marketDb = null;
        string? researchDb = null;
        string? verdictDir = null;
        var symbol = "BTCUSDT";
        var strategy = "FixedWindow";
        var timeFrame = "1D";
        var enterBar = 5;
        var exitBar = 15;
        var takeProfitPct = 0.08m;
        var rsiExit = 70m;
        var initialCapital = 10_000m;
        var feeBps = 4m;
        var positionFraction = 0.1m;
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        var save = false;

        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a.Equals("--save", StringComparison.OrdinalIgnoreCase))
            {
                save = true;
                continue;
            }

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
            else if (a.Equals("--symbol", StringComparison.OrdinalIgnoreCase))
                symbol = TakeValue();
            else if (a.Equals("--timeframe", StringComparison.OrdinalIgnoreCase))
                timeFrame = TakeValue();
            else if (a.Equals("--from", StringComparison.OrdinalIgnoreCase))
                from = ParseUtcOffset(TakeValue(), a);
            else if (a.Equals("--to", StringComparison.OrdinalIgnoreCase))
                to = ParseUtcOffset(TakeValue(), a);
            else if (a.Equals("--strategy", StringComparison.OrdinalIgnoreCase))
                strategy = TakeValue();
            else if (a.Equals("--enter-bar", StringComparison.OrdinalIgnoreCase))
                enterBar = int.Parse(TakeValue());
            else if (a.Equals("--exit-bar", StringComparison.OrdinalIgnoreCase))
                exitBar = int.Parse(TakeValue());
            else if (a.Equals("--take-profit-pct", StringComparison.OrdinalIgnoreCase))
                takeProfitPct = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--rsi-exit", StringComparison.OrdinalIgnoreCase))
                rsiExit = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--initial-capital", StringComparison.OrdinalIgnoreCase))
                initialCapital = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--fee-bps", StringComparison.OrdinalIgnoreCase))
                feeBps = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--position-fraction", StringComparison.OrdinalIgnoreCase))
                positionFraction = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else
                throw new ArgumentException($"Unknown argument: {a}. {Usage}");
        }

        if (string.IsNullOrWhiteSpace(marketDb))
            throw new ArgumentException($"--market-db is required. {Usage}");

        researchDb ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "research.sqlite");

        return new BacktestArgs(
            Path.GetFullPath(marketDb),
            Path.GetFullPath(researchDb),
            string.IsNullOrWhiteSpace(verdictDir) ? null : Path.GetFullPath(verdictDir),
            symbol,
            strategy,
            timeFrame,
            enterBar,
            exitBar,
            takeProfitPct,
            rsiExit,
            initialCapital,
            feeBps,
            positionFraction,
            from,
            to,
            save);
    }

    private static DateTimeOffset ParseUtcOffset(string value, string flag)
    {
        if (!DateTimeOffset.TryParse(value, null, DateTimeStyles.AssumeUniversal, out var parsed))
            throw new ArgumentException($"Invalid {flag} value '{value}'; use ISO-8601 UTC (e.g. 2024-01-01T00:00:00Z).");
        return parsed.ToUniversalTime();
    }
}
