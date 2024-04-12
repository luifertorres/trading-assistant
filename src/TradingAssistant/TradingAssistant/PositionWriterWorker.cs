using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.EntityFrameworkCore;

namespace TradingAssistant
{
    public class PositionWriterWorker : BackgroundService
    {
        private readonly ILogger<PositionWriterWorker> _logger;
        private readonly IServiceScopeFactory _factory;
        private readonly BinanceService _service;

        public PositionWriterWorker(ILogger<PositionWriterWorker> logger, IServiceScopeFactory factory, BinanceService service)
        {
            _logger = logger;
            _factory = factory;
            _service = service;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _service.SubscribeToAccountUpdates(@event => HandleAccountUpdate(@event, stoppingToken));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event, CancellationToken cancellationToken = default)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                var delaySeconds = DateTimeOffset.UtcNow.Subtract(@event.Data.EventTime).TotalSeconds;

                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    _logger.LogInformation("Saving {Symbol} position {DelaySeconds:F1} seconds later",
                        position.Symbol,
                        delaySeconds);

                    await SavePosition(position, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Deleting {Symbol} position {DelaySeconds:F1} seconds later",
                        position.Symbol,
                        delaySeconds);

                    await DeletePosition(position, cancellationToken);
                }
            }
        }

        private async ValueTask SavePosition(BinanceFuturesStreamPosition position, CancellationToken cancellationToken = default)
        {
            if (!_service.TryGetLeverage(position.Symbol, out var leverage))
            {
                return;
            }

            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var openPosition = await database.OpenPositions.FirstOrDefaultAsync(p => p.Symbol == position.Symbol,
                cancellationToken);

            var breakEvenPrice = TakeProfitPrice.Calculate(position.EntryPrice,
                position.Quantity,
                offset: 0,
                includeFees: true);

            if (openPosition is null)
            {
                await database.OpenPositions.AddAsync(new()
                {
                    Symbol = position.Symbol,
                    Leverage = leverage,
                    PositionSide = position.PositionSide,
                    EntryPrice = position.EntryPrice,
                    Quantity = position.Quantity,
                    BreakEvenPrice = breakEvenPrice,
                    UpdateTime = DateTimeOffset.UtcNow,
                },
                cancellationToken);
            }
            else
            {
                openPosition.EntryPrice = position.EntryPrice;
                openPosition.Quantity = position.Quantity;
                openPosition.BreakEvenPrice = breakEvenPrice;
                openPosition.UpdateTime = DateTimeOffset.UtcNow;
            }

            await database.SaveChangesAsync(cancellationToken);
        }

        private async Task DeletePosition(BinanceFuturesStreamPosition position, CancellationToken cancellationToken)
        {
            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var openPosition = await database.OpenPositions.FirstOrDefaultAsync(p => p.Symbol == position.Symbol,
                cancellationToken);

            if (openPosition is not null)
            {
                database.OpenPositions.Remove(openPosition);

                await database.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
