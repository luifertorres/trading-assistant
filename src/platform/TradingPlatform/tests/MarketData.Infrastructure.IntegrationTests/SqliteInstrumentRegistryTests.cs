using FluentAssertions;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Infrastructure.IntegrationTests;

public sealed class SqliteInstrumentRegistryTests
{
    [Fact]
    public async Task UpsertAsync_SameNaturalKey_ReturnsStableInstrumentId()
    {
        var dbPath = NewDbPath();
        using var provider = CreateProvider(dbPath);
        var registry = provider.GetRequiredService<IInstrumentRegistry>();
        var upsert = SampleUpsert("BTCUSDT");

        var first = await registry.UpsertAsync(upsert);
        var second = await registry.UpsertAsync(upsert with { SeenAtUtc = DateTimeOffset.UtcNow.AddMinutes(1) });

        second.Should().Be(first);
    }

    [Fact]
    public async Task UpsertAsync_DistinctNaturalKeys_ReturnDistinctInstrumentIds()
    {
        var dbPath = NewDbPath();
        using var provider = CreateProvider(dbPath);
        var registry = provider.GetRequiredService<IInstrumentRegistry>();

        var btc = await registry.UpsertAsync(SampleUpsert("BTCUSDT"));
        var eth = await registry.UpsertAsync(SampleUpsert("ETHUSDT"));

        eth.Should().NotBe(btc);
    }

    private static ServiceProvider CreateProvider(string dbPath)
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

    private static string NewDbPath() =>
        Path.Combine(Path.GetTempPath(), $"market-{Guid.NewGuid():N}.sqlite");
}
