using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingAssistant.Application;

namespace TradingAssistant.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: register DbContext, repositories, Binance exchange service
        services.AddDbContext<TradingContext>();
        services.AddSingleton<ITradingSignalQueue, TradingSignalQueue>();
        return services;
    }
}


