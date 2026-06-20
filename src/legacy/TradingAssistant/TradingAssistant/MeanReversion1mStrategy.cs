using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure;

#if DISABLED_STRATEGIES
namespace TradingAssistant
{
    internal class MeanReversion1mStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
                return Task.CompletedTask;
            if (notification.LastCandle.Interval != KlineInterval.OneMinute)
            {
            }

            var maybeOrderSide = GetReversionSignal(notification.SmasHigherTimeFrame, notification.Smas, notification.Rsis);

            if (maybeOrderSide.HasValue)
            {
                var orderSide = maybeOrderSide.Value;
                var tradingSignalNotification = new TradingSignalNotification(notification.LastCandle.Symbol,
                    notification.LastCandle.Interval,
                    notification.LastCandle.OpenTime,
                    orderSide.AsPositionSide(),
                    orderSide,
                    notification.LastCandle.ClosePrice);

                return publisher.Publish(tradingSignalNotification, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private static OrderSide? GetReversionSignal(double[][] smasHigherTimeFrame, double[][] smas, double[][] rsis)
        {
            var fastSmasHigherTimeFrame = smasHigherTimeFrame.Take(4);
            var fastSmas = smas.Take(1);
            var slowSmas = smas.Skip(fastSmas.Count());

            if (fastSmas.Any(sma => sma.Length < 1))
            {
                return null;
            }

            var areFastSmasHigherTimeFrameOrderedFromSlowToFast = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasDowntrending = slowSmas.TakeLast(1).AreDowntrending();
            var areSlowSmasOrderedFromSlowToFast = slowSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var wereRsisOrderedFromFastToSlow = rsis.WereOrderedFromFastToSlow(lookbackPeriods: 2);
            var areRsisOrderedFromFastToSlow = rsis.PickLatestValues().AreOrderedFromFastToSlow();

            if (areFastSmasHigherTimeFrameOrderedFromSlowToFast
                && areSlowSmasDowntrending
                && areSlowSmasOrderedFromSlowToFast
                && !wereRsisOrderedFromFastToSlow
                && areRsisOrderedFromFastToSlow)
            {
                return OrderSide.Buy;
            }

            var areFastSmasHigherTimeFrameOrderedFromFastToSlow = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasUptrending = slowSmas.TakeLast(1).AreUptrending();
            var areSlowSmasOrderedFromFastToSlow = slowSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var wereRsisOrderedFromSlowToFast = rsis.WereOrderedFromSlowToFast(lookbackPeriods: 2);
            var areRsisOrderedFromSlowToFast = rsis.PickLatestValues().AreOrderedFromSlowToFast();

            if (areFastSmasHigherTimeFrameOrderedFromFastToSlow
                && areSlowSmasUptrending
                && areSlowSmasOrderedFromFastToSlow
                && !wereRsisOrderedFromSlowToFast
                && areRsisOrderedFromSlowToFast)
            {
                return OrderSide.Sell;
            }

            return null;
        }
    }
}
#endif
