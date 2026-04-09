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
}
