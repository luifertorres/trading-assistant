using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure;

#if DISABLED_STRATEGIES
namespace TradingAssistant
{
    internal class MeanReversion1mOr15mStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.LastCandle.Interval is not (KlineInterval.OneMinute or KlineInterval.FifteenMinutes))
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

        private static OrderSide? GetReversionSignal(double[][] smasHigherTimeFrame, double[][] smas, double[][] rsis)
        {
            var fastSmasHigherTimeFrame = smasHigherTimeFrame.Take(4);
            var fastSmas = smas.Take(1);
            var slowSmas = smas.Skip(fastSmas.Count());

            var fastRsis = rsis.Take(3);
            var slowRsis = rsis.Skip(fastRsis.Count());

            if (fastSmas.Any(sma => sma.Length < 2) || fastRsis.Any(rsi => rsi.Length < 2))
            {
                return null;
            }

            var areFastSmasHigherTimeFrameOrderedFromSlowToFast = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromSlowToFast();
            var areSlowSmasDowntrending = slowSmas.TakeLast(1).AreDowntrending();
            var areSlowSmasOrderedFromSlowToFast = slowSmas.PickLatestValues().AreOrderedFromSlowToFast();
            var wereSlowRsisOrderedFromSlowToFast = slowRsis.WereOrderedFromSlowToFast(lookbackPeriods: 2);
            var wereFastRsisGoingDown = fastRsis.Select(rsi => rsi[^2]).All(rsi => rsi <= slowRsis.First()[^2]);
            var areFastRsisGoingUp = fastRsis.PickLatestValues().All(rsi => rsi >= slowRsis.PickLatestValues().First());

            if (areFastSmasHigherTimeFrameOrderedFromSlowToFast
                && areSlowSmasDowntrending
                && areSlowSmasOrderedFromSlowToFast
                && wereSlowRsisOrderedFromSlowToFast
                && wereFastRsisGoingDown
                && areFastRsisGoingUp)
            {
                return OrderSide.Buy;
            }

            var areFastSmasHigherTimeFrameOrderedFromFastToSlow = fastSmasHigherTimeFrame.PickLatestValues().AreOrderedFromFastToSlow();
            var areSlowSmasUptrending = slowSmas.TakeLast(1).AreUptrending();
            var areSlowSmasOrderedFromFastToSlow = slowSmas.PickLatestValues().AreOrderedFromFastToSlow();
            var wereSlowRsisOrderedFromFastToSlow = slowRsis.WereOrderedFromFastToSlow(lookbackPeriods: 2);
            var wereFastRsisGoingUp = fastRsis.Select(rsi => rsi[^2]).All(rsi => rsi >= slowRsis.First()[^2]);
            var areFastRsisGoingDown = fastRsis.PickLatestValues().All(rsi => rsi <= slowRsis.PickLatestValues().First());

            if (areFastSmasHigherTimeFrameOrderedFromFastToSlow
                && areSlowSmasUptrending
                && areSlowSmasOrderedFromFastToSlow
                && wereSlowRsisOrderedFromFastToSlow
                && wereFastRsisGoingUp
                && areFastRsisGoingDown)
            {
                return OrderSide.Sell;
            }

            return null;
        }
    }
}
#endif
