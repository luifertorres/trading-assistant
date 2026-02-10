using CryptoExchange.Net.Authentication;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure;
using X.Extensions.Logging.Telegram.Extensions;

namespace TradingAssistant
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders()
                        .AddTelegram(context.Configuration)
                        .AddConsole();
                }).ConfigureServices((context, services) =>
                {
                    services.AddApplication();
                    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
                    services.AddBinance(options =>
                    {
                        var key = context.Configuration["Binance:Futures:ApiKey"]!;
                        var secret = context.Configuration["Binance:Futures:ApiSecret"]!;

                        options.ApiCredentials = new ApiCredentials(key, secret);
                        options.Rest.RequestTimeout = Timeout.InfiniteTimeSpan;
                    });

                    services.AddInfrastructure(context.Configuration);

                    services.AddHostedService<TradingSignalWorker>();
                    services.AddHostedService<PositionWriterWorker>();
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<Rsi5RealtimeIndicatorWorker>();
                    //services.AddHostedService<BreakEvenWorker>();
                    //services.AddHostedService<TakeProfitManager>();
                    //services.AddHostedService<SteppedTrailingStopManager>();
                    //services.AddHostedService<TrailingStopManager>();
                    //services.AddHostedService<Rsi200ClosePositionWorker>();
                })
                .Build();

            host.Services.GetRequiredService<BinanceService>()
                .TriggerLastCandleClosedNotifications();

            host.Run();
        }
    }
}
