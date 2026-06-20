using Binance.Net;
using MarketData.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers instrument registry and canonical candles store against a shared SQLite connection.</summary>
    public static IServiceCollection AddMarketDataSqlite(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton(_ => new SqliteMarketDatabase(databasePath));
        services.AddSingleton<SqliteInstrumentRegistry>();
        services.AddSingleton<IInstrumentRegistry>(sp => sp.GetRequiredService<SqliteInstrumentRegistry>());
        services.AddSingleton<SqliteCandleStore>();
        services.AddSingleton<ICandleSeriesReader>(sp => sp.GetRequiredService<SqliteCandleStore>());
        services.AddSingleton<ICandleSeriesWriter>(sp => sp.GetRequiredService<SqliteCandleStore>());
        return services;
    }

    /// <summary>USD-M 1d backfill: Binance.Net REST with infinite HTTP timeout, checkpoint store, and orchestrator.</summary>
    public static IServiceCollection AddMarketDataBinanceUsdM1dBackfill(this IServiceCollection services, string checkpointFilePath)
    {
        services.AddBinance(options => { options.Rest.RequestTimeout = Timeout.InfiniteTimeSpan; });
        services.AddSingleton<IBackfillCheckpointStore>(sp =>
            new BackfillCheckpointJsonStore(checkpointFilePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BackfillCheckpointJsonStore>>()));
        services.AddSingleton<IUsdM1dBackfillExchange, BinanceUsdM1dBackfillExchange>();
        services.AddSingleton<Usdm1dBackfillOrchestrator>();
        return services;
    }
}
