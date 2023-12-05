namespace TradingAssistant
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<StopLossManager>();
                    services.AddHostedService<TakeProfitManager>();
                })
                .Build();

            host.Run();
        }
    }
}
