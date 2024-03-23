using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
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
                }).ConfigureServices((context, services) =>
                {
                    services.AddMediatR(configuration =>
                    {
                        configuration.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    });

                    services.AddBinance(restOptions =>
                    {
                        var key = context.Configuration["Binance:Futures:ApiKey"]!;
                        var secret = context.Configuration["Binance:Futures:ApiSecret"]!;

                        restOptions.ApiCredentials = new ApiCredentials(key, secret);

                        const int RateLimitPeriod = 1;
                        const int Limit = 2400 / (60 / RateLimitPeriod);

                        var perTimePeriod = TimeSpan.FromSeconds(RateLimitPeriod);
                        var totalRateLimiter = new RateLimiter().AddTotalRateLimit(Limit, perTimePeriod);

                        restOptions.UsdFuturesOptions.RateLimiters.Add(totalRateLimiter);
                    },
                    socketOptions =>
                    {
                        var key = context.Configuration["Binance:Futures:ApiKey"]!;
                        var secret = context.Configuration["Binance:Futures:ApiSecret"]!;

                        socketOptions.ApiCredentials = new ApiCredentials(key, secret);
                    });

                    services.AddDbContext<TradingContext>();

                    services.AddSingleton<BinanceService>();
                    services.AddSingleton<Rsi200SignalGenerator>();

                    services.AddHostedService<SignalsWorker>();
                    services.AddHostedService<PositionWriterWorker>();
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<BreakEvenWorker>();
                    services.AddHostedService<TakeProfitManager>();
                    services.AddHostedService<SteppedTrailingStopManager>();
                    services.AddHostedService<TrailingStopManager>();
                    services.AddHostedService<Rsi200ClosePositionWorker>();
                })
                .Build();

            host.Run();
        }
    }
}
