using Microsoft.Extensions.DependencyInjection;
using Research.Application;

namespace Research.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResearchInfrastructure(this IServiceCollection services, string researchDatabasePath)
    {
        services.AddSingleton<ITradingStrategyFactory, DefaultTradingStrategyFactory>();
        services.AddSingleton<IBacktestRunner, BacktestRunner>();
        services.AddSingleton(_ => new SqliteSimulationRunRepository(researchDatabasePath));
        services.AddSingleton<ISimulationRunRepository>(sp => sp.GetRequiredService<SqliteSimulationRunRepository>());
        return services;
    }
}
