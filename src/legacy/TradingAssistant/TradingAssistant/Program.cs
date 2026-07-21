using Binance.Net;
using FASTER.core;
using Microsoft.EntityFrameworkCore;
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
                        .AddConsole();

                    var telegramToken = context.Configuration["Logging:Telegram:AccessToken"];
                    var telegramChatId = context.Configuration["Logging:Telegram:ChatId"];
                    if (!string.IsNullOrWhiteSpace(telegramToken)
                        && !string.Equals(telegramToken, "TELEGRAM_BOT_ACCESS_TOKEN", StringComparison.Ordinal)
                        && !string.IsNullOrWhiteSpace(telegramChatId))
                    {
                        builder.AddTelegram(context.Configuration);
                    }
                }).ConfigureServices((context, services) =>
                {
                    services.AddMediatR(configuration =>
                    {
                        configuration.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    });

                    services.AddBinance(options =>
                    {
                        var key = context.Configuration["Binance:Futures:ApiKey"]!;
                        var secret = context.Configuration["Binance:Futures:ApiSecret"]!;

                        // Binance.Net rate-limits/retries REST internally; do not cut off waits with HttpClient timeout.
                        options.Rest.RequestTimeout = Timeout.InfiniteTimeSpan;
                        options.ApiCredentials = new BinanceCredentials(key, secret);
                    });

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

                    services.AddSingleton<BinanceService>();
                    services.AddSingleton<TradingSignalQueueService>();

                    services.AddHostedService<TradingSignalWorker>();
                    services.AddHostedService<PositionWriterWorker>();
                    services.AddHostedService<StopLossManager>();
                    //services.AddHostedService<BreakEvenWorker>();
                    //services.AddHostedService<TakeProfitManager>();
                    //services.AddHostedService<SteppedTrailingStopManager>();
                    //services.AddHostedService<TrailingStopManager>();
                    //services.AddHostedService<Rsi200ClosePositionWorker>();
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<TradingContext>().Database.Migrate();
            }

            host.Services.GetRequiredService<BinanceService>()
                .TriggerLastCandleClosedNotifications();

            host.Run();
        }
    }
}
