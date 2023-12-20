using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class TrailingStopManager(ILogger<TrailingStopManager> logger, BinanceService binanceService) : BackgroundService
    {
        private readonly ILogger<TrailingStopManager> _logger = logger;
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
                    if (!_binanceService.TryGetLeverage(position.Symbol, out var leverage))
                    {
                        continue;
                    }

                    await PlaceTrailingStopAsync(position, leverage, cancellationToken);
                }
                else
                {
                    await _binanceService.CancelAllOrdersAsync(position.Symbol, cancellationToken);
                }
            }
        }

        private async Task PlaceTrailingStopAsync(BinanceFuturesStreamPosition position, int leverage, CancellationToken cancellationToken = default)
        {
            var roi = 200m;
            var distance = roi / leverage;

            var isTrailingStopPlaced = await _binanceService.TryPlaceTrailingStopAsync(position.Symbol,
                position.Quantity.AsOrderSide().Reverse(),
                position.Quantity,
                callbackRate: distance,
                cancellationToken: cancellationToken);

            if (!isTrailingStopPlaced)
            {
                await _binanceService.TryCancelTrailingStopAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceTrailingStopAsync(position.Symbol,
                    position.Quantity.AsOrderSide().Reverse(),
                    position.Quantity,
                    callbackRate: distance,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
