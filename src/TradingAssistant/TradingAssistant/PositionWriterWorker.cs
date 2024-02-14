using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace TradingAssistant
{
    public class PositionWriterWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _factory;
        private readonly BinanceService _service;

        public PositionWriterWorker(IServiceScopeFactory factory, BinanceService service)
        {
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
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    await SavePosition(position, cancellationToken);
                }
                else
                {
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
