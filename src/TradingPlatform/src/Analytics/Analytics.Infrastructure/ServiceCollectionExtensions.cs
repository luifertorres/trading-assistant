using Analytics.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services) =>
        services.AddSingleton<IRunAnalytics, RunAnalyticsEngine>();
}
