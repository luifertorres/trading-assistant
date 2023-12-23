using X.Extensions.Logging.Telegram;

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
                }).ConfigureServices(services =>
                {
                    services.AddSingleton<BinanceService>();

                    services.AddHostedService<SignalsWorker>();
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<TakeProfitManager>();
                    //services.AddHostedService<SteppedTrailingStopManager>();
                    services.AddHostedService<TrailingStopManager>();
                })
                .Build();

            host.Run();
        }
    }
}
