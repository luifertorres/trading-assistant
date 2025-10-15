using System.Collections.Concurrent;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace TradingAssistant
{
    public class SteppedTrailingStopManager : BackgroundService
    {
        private readonly ILogger<SteppedTrailingStopManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly IExchangeService _exchange;
        private readonly ConcurrentDictionary<string, UpdatePriceTask> _updateTakeProfitTasks = [];

        public SteppedTrailingStopManager(ILogger<SteppedTrailingStopManager> logger,
            IConfiguration configuration,
            IServiceScopeFactory factory,
            IExchangeService exchange)
        {
            _logger = logger;
            _configuration = configuration;
            _factory = factory;
            _exchange = exchange;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _exchange.SubscribeToAccountUpdates(@event => HandleAccountUpdate(@event, stoppingToken));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event, CancellationToken cancellationToken = default)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    if (!_exchange.TryGetLeverage(position.Symbol, out var leverage))
                    {
                        continue;
                    }

                    if (_updateTakeProfitTasks.TryRemove(position.Symbol, out var updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();
                    }

                    var stepRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:SteppedTrailingStepRoi");
                    var detectionOffsetRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:SteppedTrailingDetectionOffsetRoi");
                    var trailingStop = new SteppedTrailingStop(position.EntryPrice,
                        position.Quantity,
                        leverage,
                        stepRoi,
                        detectionOffsetRoi);

                    updateTakeProfitTask = new UpdatePriceTask(position.EntryPrice, (price, token) =>
                    {
                        return UpdateSteppedTrailingStopAsync(price, position, trailingStop, token);
                    });

                    if (!_updateTakeProfitTasks.TryAdd(position.Symbol, updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();

                        continue;
                    }

                    await _exchange.TrySubscribeToPriceAsync(position.Symbol,
                        action: candle =>
                        {
                            if (position.Quantity.AsOrderSide() == OrderSide.Buy)
                            {
                                updateTakeProfitTask.UpdatePrice(candle.HighPrice);
                            }
                            else
                            {
                                updateTakeProfitTask.UpdatePrice(candle.LowPrice);
                            }
                        },
                        cancellationToken);
                }
                else
                {
                    if (_updateTakeProfitTasks.TryRemove(position.Symbol, out var updateTakeProfitTask))
                    {
                        updateTakeProfitTask.Stop();
                    }

                    await _exchange.TryUnsubscribeFromPriceAsync(position.Symbol);
                }
            }
        }

        private async Task UpdateSteppedTrailingStopAsync(decimal currentPrice,
            BinanceFuturesStreamPosition position,
            SteppedTrailingStop trailingStop,
            CancellationToken cancellationToken = default)
        {
            if (!trailingStop.TryAdvance(currentPrice, out var stopPrice))
            {
                return;
            }

            _logger.LogDebug("{Symbol} Trailing Stop advanced due to current price: {Price}", position.Symbol, currentPrice);

            var isTrailingPlaced = await _exchange.TryPlaceTakeProfitBehindAsync(position.Symbol,
                    stopPrice,
                    position.Quantity,
                    position.Quantity.AsOrderSide().Reverse(),
                    cancellationToken);

            if (!isTrailingPlaced)
            {
                await _exchange.TryCancelSteppedTrailingAsync(position.Symbol, cancellationToken);
                await _exchange.TryPlaceTakeProfitBehindAsync(position.Symbol,
                    stopPrice,
                    position.Quantity,
                    position.Quantity.AsOrderSide().Reverse(),
                    cancellationToken);
            }

            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var openPosition = await database.OpenPositions.FirstOrDefaultAsync(p => p.Symbol == position.Symbol,
                cancellationToken);

            if (openPosition is not null && !openPosition.HasStopLossInBreakEven)
            {
                openPosition.BreakEvenPrice = stopPrice;
                openPosition.HasStopLossInBreakEven = true;

                await database.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
