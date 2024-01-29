using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class SignalsWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BinanceService _binanceService;

        public SignalsWorker(IServiceProvider serviceProvider, BinanceService binanceService)
        {
            _serviceProvider = serviceProvider;
            _binanceService = binanceService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var candleClosedEvent = _binanceService.GetCandleClosedEvent();
            var signalGenerator = _serviceProvider.GetService<Rsi200SignalGenerator>();

            signalGenerator!.SubscribeTo(candleClosedEvent);

            await Task.Delay(Timeout.Infinite, stoppingToken);

            signalGenerator.Unsubscribe();
        }
    }
}
