using System.Collections.Concurrent;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class TakeProfitManager(ILogger<TakeProfitManager> logger, BinanceService binanceService) : BackgroundService
    {
        private readonly ILogger<TakeProfitManager> _logger = logger;
        private readonly BinanceService _binanceService = binanceService;
        private readonly ConcurrentDictionary<string, UpdateTakeProfitTask> _updateTakeProfitTasks = [];

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

                    if (_updateTakeProfitTasks.TryRemove(position.Symbol, out var updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();
                    }

                    var trailingStop = new TrailingStop(position.EntryPrice, position.Quantity, leverage);

                    updateTakeProfitTask = new UpdateTakeProfitTask(position.EntryPrice, (price, token) =>
                    {
                        return UpdateSteppedTrailingStopAsync(price, position, trailingStop, token);
                    });

                    if (!_updateTakeProfitTasks.TryAdd(position.Symbol, updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();

                        continue;
                    }

                    await _binanceService.TrySubscribeToPriceAsync(position.Symbol,
                        action: updateTakeProfitTask.UpdatePrice,
                        cancellationToken);
                }
                else
                {
                    if (_updateTakeProfitTasks.TryRemove(position.Symbol, out var updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();
                    }

                    await _binanceService.TryUnsubscribeFromPriceAsync(position.Symbol);
                }
            }
        }

        private async Task UpdateSteppedTrailingStopAsync(decimal currentPrice,
            BinanceFuturesStreamPosition position,
            TrailingStop trailingStop,
            CancellationToken cancellationToken = default)
        {
            if (!trailingStop.TryAdvance(currentPrice, out var stopPrice))
            {
                return;
            }

            _logger.LogDebug("{Symbol} Trailing Stop advanced due to current price: {Price}", position.Symbol, currentPrice);

            await _binanceService.TryCancelTakeProfitAsync(position.Symbol, cancellationToken);
            await _binanceService.TryPlaceTakeProfitAsync(position.Symbol,
                stopPrice,
                position.Quantity.AsOrderSide().Reverse(),
                cancellationToken);
        }
    }
}
