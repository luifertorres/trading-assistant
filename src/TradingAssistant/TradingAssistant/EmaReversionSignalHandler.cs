using MediatR;

namespace TradingAssistant
{
    public class EmaReversionSignalHandler(BinanceService binanceService) : INotificationHandler<EmaReversionSignal>
    {
        private readonly BinanceService _binanceService = binanceService;

        public async Task Handle(EmaReversionSignal notification, CancellationToken cancellationToken)
        {
            var position = await _binanceService.TryGetPositionInformationAsync(notification.Symbol, cancellationToken);

            if (position is not null)
            {
                return;
            }

            var openOrders = await _binanceService.TryGetOpenOrdersAsync(notification.Symbol, cancellationToken);

            if (openOrders.Any())
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

            const decimal MarginPercentage = 0.1m;

            var riskPercentage = MarginPercentage * leverage;
            var notional = account.AvailableBalance * riskPercentage / 100;
            var quantity = notional / notification.EntryPrice;

            await _binanceService.TryPlaceEntryOrderAsync(notification.Symbol,
                notification.Side,
                quantity,
                notification.EntryPrice,
                cancellationToken);
        }
    }
}
