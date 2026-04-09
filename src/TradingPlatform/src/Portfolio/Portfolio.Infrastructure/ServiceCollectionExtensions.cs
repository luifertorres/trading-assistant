using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application;

namespace Portfolio.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortfolioInfrastructure(this IServiceCollection services, string portfolioDirectory)
    {
        services.AddSingleton<IPortfolioComposer, PortfolioComposer>();
        services.AddSingleton(_ => new FilePortfolioRepository(portfolioDirectory));
        return services;
    }
}
