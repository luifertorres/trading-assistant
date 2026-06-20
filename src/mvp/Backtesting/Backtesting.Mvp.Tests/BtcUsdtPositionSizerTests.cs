using Binance.Net.Enums;

namespace Backtesting.Mvp.Tests;

public class BtcUsdtPositionSizerTests
{
    [Fact]
    public void Two_percent_of_5k_below_min_notional_uses_134_usd_floor()
    {
        var config = new BacktestConfig(
            "BTCUSDT",
            KlineInterval.OneHour,
            5_000m,
            0m,
            0m,
            SizePositionByInitialCapitalFraction: true,
            PositionNotionalFractionOfInitial: 0.02m,
            MinNotionalUsd: 134m,
            MinOrderQuantityBtc: 0.002m,
            QuantityStepBtc: 0.001m);

        var qty = BtcUsdtPositionSizer.QuantityForLong(100m, config);

        Assert.True(qty >= 0.002m);
        Assert.Equal(0m, qty % 0.001m);
        Assert.True(qty * 100m >= 134m);
    }
}
