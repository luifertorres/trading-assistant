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
                    services.AddMediatR(configuration =>
                    {
                        configuration.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    });

                    services.AddSingleton<BinanceService>();
                    services.AddSingleton<Rsi200SignalGenerator>();

                    services.AddHostedService<SignalsWorker>();
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<TakeProfitManager>();
                    services.AddHostedService<SteppedTrailingStopManager>();
                    services.AddHostedService<TrailingStopManager>();
                    services.AddHostedService<Rsi200ClosePositiontWorker>();
                })
                .Build();

            host.Run();
        }
    }
}
