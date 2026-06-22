using System.Globalization;
using System.Text.Json;

namespace TradingPlatform.Cli;

internal static class CohortSymbols
{
    public static readonly string[] Default =
    [
        "DOGEUSDT",
        "XRPUSDT",
        "SOLUSDT",
        "1000PEPEUSDT"
    ];
}

internal sealed record CohortBacktestArgs(
    string MarketDatabasePath,
    string ResearchDatabasePath,
    string VerdictDirectory,
    IReadOnlyList<string> Symbols,
    decimal InitialCapital,
    decimal FeeBpsPerSide,
    decimal PositionNotionalFraction,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public const string Usage =
        "cohort-backtest --market-db <path> [--research-db <path>] [--verdict-dir <path>] " +
        "[--symbols DOGEUSDT,XRPUSDT,...] [--from iso] [--to iso]";

    public static CohortBacktestArgs Parse(string[] args)
    {
        string? marketDb = null;
        string? researchDb = null;
        string? verdictDir = null;
        var symbols = CohortSymbols.Default;
        var initialCapital = 10_000m;
        var feeBps = 4m;
        var positionFraction = 0.1m;
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
            else if (a.Equals("--position-fraction", StringComparison.OrdinalIgnoreCase))
                positionFraction = decimal.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else
                throw new ArgumentException($"Unknown argument: {a}. {Usage}");
        }

        if (string.IsNullOrWhiteSpace(marketDb))
            throw new ArgumentException($"--market-db is required. {Usage}");

        researchDb ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "research.sqlite");
        verdictDir ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "verdicts");

        return new CohortBacktestArgs(
            Path.GetFullPath(marketDb),
            Path.GetFullPath(researchDb),
            Path.GetFullPath(verdictDir),
            symbols,
            initialCapital,
            feeBps,
            positionFraction,
            from,
            to);
    }
}

internal sealed record CohortComposeArgs(
    string ResearchDatabasePath,
    string PortfolioDirectory,
    string VerdictDirectory,
    double MaxPairwiseCorrelation,
    string PortfolioName)
{
    public const string Usage =
        "cohort-compose --verdict-dir <path> [--portfolio-dir <path>] [--max-corr 0.5] [--name cohort-live]";

    public static CohortComposeArgs Parse(string[] args)
    {
        string? researchDb = null;
        string? portfolioDir = null;
        string? verdictDir = null;
        var maxCorr = 0.5;
        var name = "cohort-live";

        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            string TakeValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value after {a}");
                return args[++i];
            }

            if (a.Equals("--research-db", StringComparison.OrdinalIgnoreCase))
                researchDb = TakeValue();
            else if (a.Equals("--portfolio-dir", StringComparison.OrdinalIgnoreCase))
                portfolioDir = TakeValue();
            else if (a.Equals("--verdict-dir", StringComparison.OrdinalIgnoreCase))
                verdictDir = TakeValue();
            else if (a.Equals("--max-corr", StringComparison.OrdinalIgnoreCase))
                maxCorr = double.Parse(TakeValue(), CultureInfo.InvariantCulture);
            else if (a.Equals("--name", StringComparison.OrdinalIgnoreCase))
                name = TakeValue();
            else
                throw new ArgumentException($"Unknown argument: {a}. {Usage}");
        }

        if (string.IsNullOrWhiteSpace(verdictDir))
            throw new ArgumentException($"--verdict-dir is required. {Usage}");

        researchDb ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "research.sqlite");
        portfolioDir ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "portfolios");

        return new CohortComposeArgs(
            Path.GetFullPath(researchDb),
            Path.GetFullPath(portfolioDir),
            Path.GetFullPath(verdictDir),
            maxCorr,
            name);
    }
}

internal sealed record FireTestOrderArgs(string Symbol, string MarketDatabasePath, string VerdictDirectory, bool Arm)
{
    public const string Usage = "fire-test-order --symbol <S> --market-db <path> [--verdict-dir <path>] [--arm]";

    public static FireTestOrderArgs Parse(string[] args)
    {
        string? symbol = null;
        string? marketDb = null;
        string? verdictDir = null;
        var arm = false;

        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a.Equals("--arm", StringComparison.OrdinalIgnoreCase))
            {
                arm = true;
                continue;
            }

            string TakeValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value after {a}");
                return args[++i];
            }

            if (a.Equals("--symbol", StringComparison.OrdinalIgnoreCase))
                symbol = TakeValue();
            else if (a.Equals("--market-db", StringComparison.OrdinalIgnoreCase))
                marketDb = TakeValue();
            else if (a.Equals("--verdict-dir", StringComparison.OrdinalIgnoreCase))
                verdictDir = TakeValue();
            else
                throw new ArgumentException($"Unknown argument: {a}. {Usage}");
        }

        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(marketDb))
            throw new ArgumentException(Usage);

        verdictDir ??= Path.Combine(Environment.CurrentDirectory, ".trading-platform-data", "verdicts");
        return new FireTestOrderArgs(symbol, Path.GetFullPath(marketDb), Path.GetFullPath(verdictDir), arm);
    }
}
