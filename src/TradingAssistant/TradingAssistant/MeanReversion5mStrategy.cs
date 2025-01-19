using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    internal class MeanReversion5mStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        private const KlineInterval FiveMinutesInterval = KlineInterval.FiveMinutes;
        private const int RsiPatternLookbackPeriods = 60 * 60 * 24 / (int)FiveMinutesInterval;

        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.LastCandle.Interval != FiveMinutesInterval)
            {
                return Task.CompletedTask;
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

        private OrderSide? GetReversionSignal(double[][] smasHigherTimeFrame, double[][] smas, double[][] rsis)
        {
            var fastSmasHigherTimeFrame = smasHigherTimeFrame;
            var fastSmas = smas.Take(1);
            var slowSmas = smas.Skip(fastSmas.Count());

            if (fastSmas.Any(sma => sma.Length < 1))
            {
                return null;
            }

            if (fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromSlowToFast()
                && slowSmas.TakeLast(1).AreDowntrending()
                && slowSmas.PickLatestValues().AreOrderedFromSlowToFast()
                && rsis.WereOrderedFromSlowToFast(RsiPatternLookbackPeriods)
                && !rsis.WereOrderedFromFastToSlow(lookbackPeriods: 2)
                && rsis.PickLatestValues().AreOrderedFromFastToSlow())
            {
                return OrderSide.Buy;
            }

            if (fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromFastToSlow()
                && slowSmas.TakeLast(1).AreUptrending()
                && slowSmas.PickLatestValues().AreOrderedFromFastToSlow()
                && rsis.WereOrderedFromFastToSlow(RsiPatternLookbackPeriods)
                && !rsis.WereOrderedFromSlowToFast(lookbackPeriods: 2)
                && rsis.PickLatestValues().AreOrderedFromSlowToFast())
            {
                return OrderSide.Sell;
            }

            return null;
        }
    }
}
