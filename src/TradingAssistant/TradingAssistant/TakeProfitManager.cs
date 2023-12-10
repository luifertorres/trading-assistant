using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class TakeProfitManager(BinanceService binanceService) : BackgroundService
    {
        private readonly BinanceService _binanceService = binanceService;
        private readonly object _synchronizer = new();
        private Task _updateTakeProfitTask = Task.CompletedTask;

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

                    var trailingStop = new TrailingStop(position.EntryPrice, position.Quantity, leverage);

                    await _binanceService.TrySubscribeToPriceAsync(position.Symbol,
                        action: price => UpdateTakeProfit(price, position, trailingStop, cancellationToken),
                        cancellationToken);
                }
                else
                {
                    await _binanceService.TryUnsubscribeFromPriceAsync(position.Symbol);
                }
            }
        }

        private void UpdateTakeProfit(decimal currentPrice, BinanceFuturesStreamPosition position, TrailingStop trailingStop, CancellationToken cancellationToken = default)
        {
            lock (_synchronizer)
            {
                if (!_updateTakeProfitTask.IsCompleted)
                {
                    return;
                }

                _updateTakeProfitTask = Task.Run(async () =>
                {
                    await UpdateTrailingStop(currentPrice, position, trailingStop, cancellationToken);
                },
                    cancellationToken);
            }
        }

        private async Task UpdateTrailingStop(decimal currentPrice, BinanceFuturesStreamPosition position, TrailingStop trailingStop, CancellationToken cancellationToken)
        {
            if (!trailingStop.TryAdvance(currentPrice, out var stopPrice))
            {
                return;
            }

            await _binanceService.TryCancelTakeProfitAsync(position.Symbol, cancellationToken);
            await _binanceService.TryPlaceTakeProfitAsync(position.Symbol,
                stopPrice!.Value,
                position.Quantity.AsOrderSide().Reverse(),
                cancellationToken);
        }
    }
}
