using Execution.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Execution.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExecutionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILiveOrderIntentSink, LoggingLiveOrderIntentSink>();
        services.AddSingleton<PortfolioExecutionRouter>();
        services.AddSingleton<BrokerAntiCorruptionStub>();
        return services;
    }
}
