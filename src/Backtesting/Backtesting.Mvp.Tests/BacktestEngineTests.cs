using Backtesting.Mvp;
using Binance.Net.Enums;

namespace Backtesting.Mvp.Tests;

public class BacktestEngineTests
{
    [Fact]
    public void Oscillating_series_produces_trades_and_matches_trade_count_in_report()
    {
        var klines = SyntheticKlineSeries.OscillatingUsd(
            500,
            new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1));

        var config = new BacktestConfig(
            "BTCUSDT",
            KlineInterval.OneHour,
            10_000m,
            0.01m,
            4m);

        var run = new BacktestEngine(config).Run(new InMemoryKlineSource(klines));

        Assert.Equal(klines.Count, run.EquityCurve.Count);
        Assert.True(run.Trades.Count >= 1, "Expected at least one round-trip from synthetic oscillation");

        var report = MetricsCalculator.Build(run);
        Assert.Equal(run.Trades.Count, report.TotalTrades);
        Assert.True(report.FinalEquity > 0);
        Assert.True(report.MaxDrawdownPercent >= 0);
    }

    [Fact]
    public void Flat_prices_after_warmup_yield_zero_trades()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var list = new List<MockBinanceKline>();
        for (var i = 0; i < 400; i++)
        {
            var open = start.AddHours(i);
            list.Add(MockBinanceKline.Create(open, open.AddHours(1), 100m, 100.01m, 99.99m, 100m));
        }

        var config = new BacktestConfig(
            "ETHUSDT",
            KlineInterval.OneHour,
            5_000m,
            0.1m,
            0m);

        var run = new BacktestEngine(config).Run(new InMemoryKlineSource(list));

        Assert.Empty(run.Trades);
        var report = MetricsCalculator.Build(run);
        Assert.Equal(0, report.TotalTrades);
        Assert.Equal(5_000m, report.FinalEquity);
    }

    [Fact]
    public void Mean_reversion_cycles_yield_trades_after_rsi_warmup()
    {
        var klines = SyntheticKlineSeries.MeanReversionLongCycles(
            80,
            new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1));

        var config = new BacktestConfig(
            "BTCUSDT",
            KlineInterval.OneHour,
            10_000m,
            0.01m,
            4m);

        var run = new BacktestEngine(config).Run(new InMemoryKlineSource(klines));

        Assert.True(run.Trades.Count >= 70, $"Expected most cycles to close; got {run.Trades.Count} trades");
    }
}
