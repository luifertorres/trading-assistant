using Backtesting.Mvp;
using Binance.Net.Enums;

var csvPath = args.Length >= 2 && args[0] is "--csv" or "-c"
    ? args[1]
    : null;

// ~1006 cycles → 1001 completed round-trips (warmup consumes a handful of cycles).
var klines = SyntheticKlineSeries.MeanReversionLongCycles(
    cycles: 1006,
    startUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    barDuration: TimeSpan.FromHours(1));

var config = new BacktestConfig(
    Symbol: "BTCUSDT",
    Interval: KlineInterval.OneHour,
    InitialCapital: 10_000m,
    Quantity: 0.01m,
    FeeBpsPerSide: 4m,
    RsiPeriod: 14,
    RsiOversold: 30m,
    RsiOverbought: 70m);

var source = new InMemoryKlineSource(klines);
var engine = new BacktestEngine(config);
var run = engine.Run(source);
var report = MetricsCalculator.Build(run);

Console.WriteLine(ReportFormatter.ToConsoleTable(report));
Console.WriteLine();
Console.WriteLine("Strategy: long-only RSI(14) — enter when RSI crosses up through 30; exit when RSI crosses up through 70. Fills at bar close.");

if (csvPath is not null)
{
    EquityCurveCsvWriter.Write(csvPath, run.EquityCurve);
    Console.WriteLine($"Wrote equity curve CSV: {csvPath}");
}
