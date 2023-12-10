namespace TradingAssistant
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<TakeProfitManager>();

                    services.AddSingleton<BinanceService>();
                })
                .Build();

            host.Run();
        }
    }
}
