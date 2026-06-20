using Backtesting.Mvp;
using Binance.Net.Enums;

var csvPath = ParseCsvPath(args);
var useBinance = args.Any(static a => a is "--binance" or "-b");

var config = new BacktestConfig(
    Symbol: "BTCUSDT",
    Interval: KlineInterval.OneHour,
    InitialCapital: 5_000m,
    Quantity: 0m,
    FeeBpsPerSide: 4m,
    RsiPeriod: 14,
    RsiOversold: 30m,
    RsiOverbought: 70m,
    SizePositionByInitialCapitalFraction: true,
    PositionNotionalFractionOfInitial: 0.02m,
    MinNotionalUsd: 134m,
    MinOrderQuantityBtc: 0.002m,
    QuantityStepBtc: 0.001m);

IBacktestKlineSource source;
if (useBinance)
{
    using var client = BinanceUsdFuturesKlineSource.CreateRestClientForBacktest();
    var binanceSource = await BinanceUsdFuturesKlineSource.LoadBtcUsdt1HourAsync(client, cancellationToken: default)
        .ConfigureAwait(false);
    Console.WriteLine($"Loaded {binanceSource.GetKlines().Count} BTCUSDT 1h USD-M klines from Binance.");
    source = binanceSource;
}
else
{
    // ~1006 cycles → 1001 completed round-trips (warmup consumes a handful of cycles).
    var klines = SyntheticKlineSeries.MeanReversionLongCycles(
        cycles: 1006,
        startUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        barDuration: TimeSpan.FromHours(1));
    source = new InMemoryKlineSource(klines);
}

var engine = new BacktestEngine(config);
var run = engine.Run(source);
var report = MetricsCalculator.Build(run);

Console.WriteLine(ReportFormatter.ToConsoleTable(report));
Console.WriteLine();
Console.WriteLine(
    "Strategy: long-only RSI(14) — enter when RSI crosses up through 30; exit when RSI crosses up through 70. Fills at bar close. " +
    "Position: 2% of initial notional target, min 134 USDT notional, qty step 0.001 BTC, min qty 0.002 BTC.");

if (csvPath is not null)
{
    EquityCurveCsvWriter.Write(csvPath, run.EquityCurve);
    Console.WriteLine($"Wrote equity curve CSV: {csvPath}");
}

static string? ParseCsvPath(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--csv" or "-c")
            return args[i + 1];
    }

    return null;
}
