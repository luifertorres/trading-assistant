using Binance.Net;
using FluentAssertions;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketData.Infrastructure.IntegrationTests;

/// <summary>Live Binance USD-M REST; skipped unless RUN_TRADINGPLATFORM_LIVE_BINANCE is set (see README).</summary>
public sealed class BinanceUsdM1dBackfillExchangeLiveTests
{
    [Fact]
    public async Task GetDailyKlinesPageAsync_BtcUsdt_ReturnsAscendingDailyBars()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_TRADINGPLATFORM_LIVE_BINANCE")))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddBinance(options => { options.Rest.RequestTimeout = Timeout.InfiniteTimeSpan; });
        services.AddSingleton<IUsdM1dBackfillExchange, BinanceUsdM1dBackfillExchange>();

        await using var provider = services.BuildServiceProvider();
        var exchange = provider.GetRequiredService<IUsdM1dBackfillExchange>();

        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = DateTimeOffset.UtcNow;

        var bars = await exchange.GetDailyKlinesPageAsync("BTCUSDT", start, end, CancellationToken.None);

        bars.Should().NotBeEmpty();
        for (var i = 1; i < bars.Count; i++)
            bars[i].OpenTime.Should().BeAfter(bars[i - 1].OpenTime);
    }
}
