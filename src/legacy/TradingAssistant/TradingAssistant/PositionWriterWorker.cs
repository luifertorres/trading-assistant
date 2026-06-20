using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using TradingAssistant.Infrastructure;

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
            _service.SubscribeToAccountUpdates(HandleAccountUpdate);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                var delaySeconds = DateTimeOffset.UtcNow.Subtract(@event.Data.EventTime).TotalSeconds;

                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    _logger.LogInformation("Saving {Symbol} position {DelaySeconds:F1} seconds later",
                        position.Symbol,
                        delaySeconds);

                    SavePosition(position);
                }
                else
                {
                    _logger.LogInformation("Deleting {Symbol} position {DelaySeconds:F1} seconds later",
                        position.Symbol,
                        delaySeconds);

                    DeletePosition(position);
                }
            }
        }

        private void SavePosition(BinanceFuturesStreamPosition position)
        {
            if (!_service.TryGetLeverage(position.Symbol, out var leverage))
            {
                return;
            }

            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var openPosition = database.OpenPositions.FirstOrDefault(p => p.Symbol == position.Symbol);

            var breakEvenPrice = TakeProfitPrice.Calculate(position.EntryPrice,
                position.Quantity,
                offset: 0,
                includeFees: true);

            if (openPosition is null)
            {
                database.OpenPositions.Add(new()
                {
                    Symbol = position.Symbol,
                    Leverage = leverage,
                    PositionSide = position.PositionSide,
                    EntryPrice = position.EntryPrice,
                    Quantity = position.Quantity,
                    BreakEvenPrice = breakEvenPrice,
                    UpdateTime = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                openPosition.EntryPrice = position.EntryPrice;
                openPosition.Quantity = position.Quantity;
                openPosition.BreakEvenPrice = breakEvenPrice;
                openPosition.UpdateTime = DateTimeOffset.UtcNow;
            }

            database.SaveChanges();
        }

        private void DeletePosition(BinanceFuturesStreamPosition position)
        {
            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var openPosition = database.OpenPositions.FirstOrDefault(p => p.Symbol == position.Symbol);

            if (openPosition is not null)
            {
                database.OpenPositions.Remove(openPosition);

                database.SaveChanges();
            }
        }
    }
}
