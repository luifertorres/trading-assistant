using FluentAssertions;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure.IntegrationTests;

public sealed class SqliteCandleStoreTests
{
    [Fact]
    public async Task UpsertAsync_Conflict_UpdatesExistingBar()
    {
        var dbPath = NewDbPath();
        using var provider = BuildProvider(dbPath);
        {
            var registry = provider.GetRequiredService<IInstrumentRegistry>();
            var writer = provider.GetRequiredService<ICandleSeriesWriter>();
            var reader = provider.GetRequiredService<ICandleSeriesReader>();

            var instrumentId = await registry.UpsertAsync(SampleUpsert("BTCUSDT"));
            var series = new SeriesDescriptor(instrumentId, TimeFrameCode.Day1);
            var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var original = new OhlcBar(t0, t0.AddDays(1), 1m, 2m, 0.5m, 1.5m, 10m);
            var updated = original with { Close = 9m, Volume = 20m };

            await writer.UpsertAsync(series, [original]);
            await writer.UpsertAsync(series, [updated]);

            var bars = await reader.ReadAsync(series, null, null);
            bars.Should().ContainSingle();
            bars[0].Close.Should().Be(9m);
            bars[0].Volume.Should().Be(20m);
        }

    }

    [Fact]
    public async Task ReadAsync_ReturnsAscendingOpenTimeOrder()
    {
        var dbPath = NewDbPath();
        using var provider = BuildProvider(dbPath);
        {
            var registry = provider.GetRequiredService<IInstrumentRegistry>();
            var writer = provider.GetRequiredService<ICandleSeriesWriter>();
            var reader = provider.GetRequiredService<ICandleSeriesReader>();

            var instrumentId = await registry.UpsertAsync(SampleUpsert("ETHUSDT"));
            var series = new SeriesDescriptor(instrumentId, TimeFrameCode.Day1);
            var t2 = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero);
            var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var t1 = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);
            var bars = new[]
            {
                Bar(t2),
                Bar(t0),
                Bar(t1)
            };

            await writer.UpsertAsync(series, bars);

            var read = await reader.ReadAsync(series, null, null);
            read.Select(b => b.OpenTime).Should().BeInAscendingOrder();
        }

    }

    private static ServiceProvider BuildProvider(string dbPath)
    {
        var services = new ServiceCollection();
        services.AddMarketDataSqlite(dbPath);
        return services.BuildServiceProvider();
    }

    private static InstrumentUpsert SampleUpsert(string exchangeSymbol) =>
        new(
            "binance",
            "usdm",
            "perpetual",
            exchangeSymbol,
            exchangeSymbol[..^4],
            "USDT",
            exchangeSymbol,
            2,
            3,
            "[]",
            "TRADING",
            DateTimeOffset.UtcNow);

    private static OhlcBar Bar(DateTimeOffset openTime) =>
        new(openTime, openTime.AddDays(1), 1m, 2m, 0.5m, 1.5m, 100m);

    private static string NewDbPath() =>
        Path.Combine(Path.GetTempPath(), $"market-{Guid.NewGuid():N}.sqlite");
}
