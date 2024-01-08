using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class StopLossManager : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;
        private readonly decimal? _nullDecimal;

        public StopLossManager(IConfiguration configuration, BinanceService binanceService)
        {
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
            var roi = _configuration.GetValue<decimal>("Binance:RiskManagement:StopLossRoi");
            var moneyToLose = _nullDecimal;

            var isStopLossPlaced = await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                position.EntryPrice,
                position.Quantity,
                moneyToLose,
                roi,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binanceService.TryCancelStopLossAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceStopLossAsync(position.Symbol,
                    position.EntryPrice,
                    position.Quantity,
                    moneyToLose,
                    roi,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
