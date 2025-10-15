using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingAssistant.Application;
using FASTER.core;
using Microsoft.Extensions.Logging;
using TradingAssistant.Infrastructure.Faster;

namespace TradingAssistant.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: register DbContext, repositories, Binance exchange service
        services.AddDbContext<TradingContext>();
        services.AddSingleton(provider =>
        {
            var log = Devices.CreateLogDevice($"c:/temp/hlog.log");
            var objlog = Devices.CreateLogDevice("c:/temp/hlog.obj.log");
            var fasterLogger = provider.GetRequiredService<ILogger<FasterKV<CandleId, Candle>>>();

            var settings = new FasterKVSettings<CandleId, Candle>("c:/temp", logger: fasterLogger)
            {
                LogDevice = log,
                ObjectLogDevice = objlog,
                MutableFraction = 0.01,
                ConcurrencyControlMode = ConcurrencyControlMode.None,
                KeySerializer = () => new CandleIdSerializer(),
                ValueSerializer = () => new CandleSerializer(),
            };

            return new FasterKV<CandleId, Candle>(settings);
        });
        services.AddSingleton<ICandleRepository, FasterCandleRepository>();
        services.AddSingleton<ITradingSignalQueue, TradingSignalQueue>();
        return services;
    }
}


