using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using TradingAssistant.Infrastructure;

namespace TradingAssistant
{
    public class TakeProfitManager : BackgroundService
    {
        private readonly BinanceService _binance;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;

        public TakeProfitManager(IConfiguration configuration, IServiceScopeFactory factory, BinanceService binance)
        {
            _configuration = configuration;
            _factory = factory;
            _binance = binance;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binance.SubscribeToAccountUpdates(HandleAccountUpdate);
            //_binance.SubscribeToOrderUpdates(HandleOrderUpdate);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                if (position.EntryPrice == 0 || position.Quantity == 0)
                {
                    _ = _binance.CancelAllOrdersAsync(position.Symbol);
                }
            }
        }

        private void HandleOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> @event)
        {
            var order = @event.Data.UpdateData;

            if (order.ClientOrderId.Contains(order.Symbol, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (order.ExecutionType is not ExecutionType.Trade)
            {
                return;
            }

            if (order.IsReduce)
            {
                return;
            }

            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            var position = database.OpenPositions.FirstOrDefault(p => p.Symbol == order.Symbol);

            if (position is null || order.Side == position.EntrySide)
            {
                _ = UpdateTakeProfitAsync(order);
            }
        }

        private async Task UpdateTakeProfitAsync(BinanceFuturesStreamOrderUpdateData order, CancellationToken cancellationToken = default)
        {
            var roi = _configuration.GetValue<decimal>("Binance:RiskManagement:TakeProfitRoi");
            var isTakeProfitPlaced = await _binance.TryPlaceTakeProfitAsync(order.Symbol,
                order.AveragePrice,
                order.Quantity,
                roi,
                includeFees: true,
                cancellationToken);

            if (!isTakeProfitPlaced)
            {
                await _binance.TryCancelTakeProfitAsync(order.Symbol, cancellationToken);
                await _binance.TryPlaceTakeProfitAsync(order.Symbol,
                    order.AveragePrice,
                    order.Quantity,
                    roi,
                    includeFees: true,
                    cancellationToken);
            }
        }
    }
}
