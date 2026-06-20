using CandlestickData.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CandlestickData.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCandlestickApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        services.AddScoped<IntegrityCheckService>();

        return services;
    }
}
