using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class StopLossManager(BinanceService binanceService) : BackgroundService
    {
        private readonly BinanceService _binanceService = binanceService;

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
            var roi = 100m;
            var moneyToLose = 1m;
            var isStopLossPlaced = await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                position.EntryPrice,
                position.Quantity,
                moneyToLose,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binanceService.TryCancelStopLossAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                    position.EntryPrice,
                    position.Quantity,
                    moneyToLose,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
