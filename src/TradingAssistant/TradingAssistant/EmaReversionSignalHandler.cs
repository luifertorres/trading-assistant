using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class EmaReversionSignalHandler : INotificationHandler<EmaReversionSignal>
    {
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;

        public EmaReversionSignalHandler(IConfiguration configuration, BinanceService binanceService)
        {
            _configuration = configuration;
            _binanceService = binanceService;
        }

        public async Task Handle(EmaReversionSignal notification, CancellationToken cancellationToken)
        {
            var positions = await _binanceService.TryGetPositionsAsync(cancellationToken);

            if (positions.Any(p => p.Symbol == notification.Symbol))
            {
                return;
            }

            if (positions.Any(p => p.Quantity.AsOrderSide() == notification.Side))
            {
                return;
            }

            if (!_binanceService.TryGetLeverage(notification.Symbol, out var leverage))
            {
                return;
            }

            var account = await _binanceService.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return;
            }

            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var accountPercentageForEntry = accountMarginPercentage * leverage;
            var notional = account.AvailableBalance * accountPercentageForEntry / 100;
            var quantity = notional / notification.EntryPrice;

            await _binanceService.TryPlaceEntryOrderAsync(notification.Symbol,
                notification.Side,
                FuturesOrderType.Market,
                quantity,
                notification.EntryPrice,
                cancellationToken);
        }
    }
}
