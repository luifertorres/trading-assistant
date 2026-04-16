using Binance.Net;
using MarketData.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers per-series SQLite store as both reader and writer. Single connection per host lifetime.</summary>
    public static IServiceCollection AddMarketDataSqlite(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton(_ => new SqlitePerSeriesCandleStore(databasePath));
        services.AddSingleton<ICandleSeriesReader>(sp => sp.GetRequiredService<SqlitePerSeriesCandleStore>());
        services.AddSingleton<ICandleSeriesWriter>(sp => sp.GetRequiredService<SqlitePerSeriesCandleStore>());
        return services;
    }

    /// <summary>USD-M 1d backfill: Binance.Net REST with infinite HTTP timeout, checkpoint store, and orchestrator.</summary>
    public static IServiceCollection AddMarketDataBinanceUsdM1dBackfill(this IServiceCollection services, string checkpointFilePath)
    {
        services.AddBinance(options => { options.Rest.RequestTimeout = Timeout.InfiniteTimeSpan; });
        services.AddSingleton<IBackfillCheckpointStore>(_ => new BackfillCheckpointJsonStore(checkpointFilePath));
        services.AddSingleton<IUsdM1dBackfillExchange, BinanceUsdM1dBackfillExchange>();
        services.AddSingleton<Usdm1dBackfillOrchestrator>();
        return services;
    }
}
