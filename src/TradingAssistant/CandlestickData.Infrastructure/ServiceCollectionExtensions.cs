using CandlestickData.Application.Interfaces;
using CandlestickData.Infrastructure.Binance;
using CandlestickData.Infrastructure.Events;
using CandlestickData.Infrastructure.Persistence;
using CandlestickData.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CandlestickData.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCandlestickInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("CandlestickData");

        services.AddDbContext<CandlestickDataContext>(options =>
        {
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString ?? "Data Source=candlestick_data.db");
            }
        });

        services.AddScoped<ICandlestickRepository, CandlestickRepository>();
        services.AddScoped<ISyncCheckpointRepository, SyncCheckpointRepository>();
        services.AddScoped<ISymbolIntegrityRepository, SymbolIntegrityRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();
        services.AddScoped<IExchangeDataSource, BinanceDataSource>();

        var syncConfig = configuration.GetSection("Sync").Get<SyncConfiguration>() ?? new SyncConfiguration();
        services.AddSingleton(syncConfig);
        services.AddSingleton<HistoricalSyncWorker>();
        services.AddSingleton<RealtimeIngestionWorker>();
        services.AddSingleton<SyncOrchestrator>();
        services.AddSingleton<ISyncOrchestrator>(sp => sp.GetRequiredService<SyncOrchestrator>());

        services.AddSingleton<WebSocketCandleEventPublisher>();
        services.AddSingleton<ICandleEventPublisher>(sp => sp.GetRequiredService<WebSocketCandleEventPublisher>());

        return services;
    }
}
