using Binance.Net;
using Execution.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Execution.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExecutionInfrastructure(this IServiceCollection services) =>
        services.AddExecutionInfrastructure(liveTrading: false);

    public static IServiceCollection AddExecutionInfrastructure(this IServiceCollection services, bool liveTrading)
    {
        if (liveTrading)
        {
            services.AddBinance();
            services.AddOptions<LiveTradingOptions>();
            services.AddSingleton<ILiveOrderIntentSink, BinanceLiveOrderIntentSink>();
        }
        else
        {
            services.AddSingleton<ILiveOrderIntentSink, LoggingLiveOrderIntentSink>();
        }

        services.AddSingleton<PortfolioExecutionRouter>();
        services.AddSingleton<BrokerAntiCorruptionStub>();
        return services;
    }
}
