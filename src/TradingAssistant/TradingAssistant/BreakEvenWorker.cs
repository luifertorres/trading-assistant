using System.Collections.Concurrent;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class BreakEvenWorker : BackgroundService
    {
        private readonly ILogger<BreakEvenWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;
        private readonly ConcurrentDictionary<string, UpdatePriceTask> _updateBreakEvenTasks = [];

        public BreakEvenWorker(ILogger<BreakEvenWorker> logger,
            IConfiguration configuration,
            BinanceService binanceService)
        {
            _logger = logger;
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
                    if (!_binanceService.TryGetLeverage(position.Symbol, out var leverage))
                    {
                        continue;
                    }

                    if (_updateBreakEvenTasks.TryRemove(position.Symbol, out var updateBreakEvenTask))
                    {
                        updateBreakEvenTask.Stop();
                    }

                    var minRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:MinRoiBeforeBreakEven");
                    var breakEven = new BreakEven(position.EntryPrice, position.Quantity, minRoi, leverage);

                    updateBreakEvenTask = new UpdatePriceTask(position.EntryPrice, (price, token) =>
                    {
                        return UpdateBreakEvenAsync(price, position, breakEven, token);
                    });

                    if (!_updateBreakEvenTasks.TryAdd(position.Symbol, updateBreakEvenTask))
                    {
                        updateBreakEvenTask.Stop();

                        continue;
                    }

                    await _binanceService.TrySubscribeToPriceAsync(position.Symbol,
                        action: updateBreakEvenTask.UpdatePrice,
                        cancellationToken);
                }
                else
                {
                    await _binanceService.TryUnsubscribeFromPriceAsync(position.Symbol);

                    if (_updateBreakEvenTasks.TryRemove(position.Symbol, out var updateBreakEvenTask))
                    {
                        updateBreakEvenTask.Stop();
                    }
                }
            }
        }

        private async Task UpdateBreakEvenAsync(decimal currentPrice,
            BinanceFuturesStreamPosition position,
            BreakEven breakEven,
            CancellationToken cancellationToken = default)
        {
            if (!breakEven.TryGetPrice(currentPrice, out var stopPrice))
            {
                return;
            }

            _logger.LogDebug("{Symbol} Break Even will be placed due to current price: {Price}", position.Symbol, currentPrice);

            var isBreakEvenPlaced = await _binanceService.TryPlaceBreakEvenAsync(position.Symbol,
                    position.Quantity.AsOrderSide(),
                    stopPrice,
                    cancellationToken);

            if (!isBreakEvenPlaced)
            {
                await _binanceService.TryCancelBreakEvenAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceBreakEvenAsync(position.Symbol,
                    position.Quantity.AsOrderSide(),
                    stopPrice,
                    cancellationToken);
            }

            await _binanceService.TryUnsubscribeFromPriceAsync(position.Symbol);

            if (_updateBreakEvenTasks.TryRemove(position.Symbol, out var updateBreakEvenTask))
            {
                updateBreakEvenTask.Stop();
            }
        }
    }
}
