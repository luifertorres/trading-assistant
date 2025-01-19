using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    internal class TrendFollowing15mStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.LastCandle.Interval != KlineInterval.FifteenMinutes)
            {
                return Task.CompletedTask;
            }

            var maybeOrderSide = GetTrendSignal(notification.SmasHigherTimeFrame, notification.Smas, notification.Rsis);

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

        private static OrderSide? GetTrendSignal(double[][] smasHigherTimeFrame, double[][] smas, double[][] rsis)
        {
            smas = smas.Skip(1).ToArray();

            var fastSmasHigherTimeFrame = smasHigherTimeFrame.Take(4);
            var fastSmas = smas.Take(2);
            var slowSmas = smas.Skip(fastSmas.Count());

            var fastRsis = rsis.Take(2);

            if (fastSmas.Any(sma => sma.Length < 2) || fastRsis.Any(rsi => rsi.Length < 2))
            {
                return null;
            }

            var areFastSmasHigherTimeFrameOrderedFromFastToSlow = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasUptrending = slowSmas.AreUptrending();
            var areSlowSmasOrderedFromFastToSlow = slowSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var areFastSmasOrderedFromSlowToFast = fastSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var wereRsisOrderedFromSlowToFast = rsis.WereOrderedFromSlowToFast(lookbackPeriods: 2);
            var areFastRsisGoingUp = fastRsis.PickLatestValues().AreOrderedFromFastToSlow();

            if (areFastSmasHigherTimeFrameOrderedFromFastToSlow
                && areSlowSmasUptrending
                && areSlowSmasOrderedFromFastToSlow
                && areFastSmasOrderedFromSlowToFast
                && wereRsisOrderedFromSlowToFast
                && areFastRsisGoingUp)
            {
                return OrderSide.Buy;
            }

            var areFastSmasHigherTimeFrameOrderedFromSlowToFast = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasDowntrending = slowSmas.AreDowntrending();
            var areSlowSmasOrderedFromSlowToFast = slowSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var areFastSmasOrderedFromFastToSlow = fastSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var wereRsisOrderedFromFastToSlow = rsis.WereOrderedFromFastToSlow(lookbackPeriods: 2);
            var areFastRsisGoingDown = fastRsis.PickLatestValues().AreOrderedFromSlowToFast();

            if (areFastSmasHigherTimeFrameOrderedFromSlowToFast
                && areSlowSmasDowntrending
                && areSlowSmasOrderedFromSlowToFast
                && areFastSmasOrderedFromFastToSlow
                && wereRsisOrderedFromFastToSlow
                && areFastRsisGoingDown)
            {
                return OrderSide.Sell;
            }

            return null;
        }
    }
}
