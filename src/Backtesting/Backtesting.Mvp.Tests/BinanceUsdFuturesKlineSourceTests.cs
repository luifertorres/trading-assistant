using Backtesting.Mvp;

namespace Backtesting.Mvp.Tests;

public class BinanceUsdFuturesKlineSourceTests
{
    [Fact]
    public void LastCompletedOneHourBarCloseUtc_mid_hour_is_start_of_current_hour()
    {
        var utc = new DateTime(2024, 6, 15, 13, 59, 47, DateTimeKind.Utc);
        var close = BinanceUsdFuturesKlineSource.LastCompletedOneHourBarCloseUtc(utc);
        Assert.Equal(new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc), close);
    }

    [Fact]
    public void LastCompletedOneHourBarCloseUtc_exact_hour_boundary_includes_bar_that_just_closed()
    {
        var utc = new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var close = BinanceUsdFuturesKlineSource.LastCompletedOneHourBarCloseUtc(utc);
        Assert.Equal(new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc), close);
    }

    [Fact]
    public void DefaultHistoryStartUtc_is_utc()
    {
        Assert.Equal(DateTimeKind.Utc, BinanceUsdFuturesKlineSource.DefaultHistoryStartUtc.Kind);
    }
}
