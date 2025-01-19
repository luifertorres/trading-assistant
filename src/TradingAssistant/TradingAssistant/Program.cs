using CryptoExchange.Net.Authentication;
using FASTER.core;
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
                    },
                    socketOptions =>
                    {
                        var key = context.Configuration["Binance:Futures:ApiKey"]!;
                        var secret = context.Configuration["Binance:Futures:ApiSecret"]!;

                        socketOptions.ApiCredentials = new ApiCredentials(key, secret);
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
                            MutableFraction = 0.1,
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

            host.Services.GetRequiredService<BinanceService>()
                .TriggerLastCandleClosedNotifications();

            host.Run();
        }
    }
}
