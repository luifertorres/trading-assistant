using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;

namespace TradingAssistant
{
    public class StopLossManager : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;

        public StopLossManager(IConfiguration configuration, BinanceService binanceService)
        {
            _configuration = configuration;
            _binanceService = binanceService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binanceService.SubscribeToAccountUpdates(@event => HandleAccountUpdate(@event, stoppingToken));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event, CancellationToken cancellationToken = default)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    await UpdateStopLoss(position, cancellationToken);
                }
                else
                {
                    await _binanceService.CancelAllOrdersAsync(position.Symbol, cancellationToken);
                }
            }
        }

        private async Task UpdateStopLoss(BinanceFuturesStreamPosition position, CancellationToken cancellationToken = default)
        {
            var roi = _configuration.GetValue<decimal>("Binance:RiskManagement:StopLossRoi");
            var isStopLossPlaced = await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                position.EntryPrice,
                position.Quantity,
                roi,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binanceService.TryCancelStopLossAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                    position.EntryPrice,
                    position.Quantity,
                    roi,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
