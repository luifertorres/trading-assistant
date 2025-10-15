using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;

namespace TradingAssistant
{
    internal class TrendFollowing1mOr15mStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        private readonly List<KlineInterval> _allowedIntervals = [KlineInterval.OneMinute, KlineInterval.FifteenMinutes];
        private int _rsiPatternLookbackPeriods;

        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
            if (!_allowedIntervals.Contains(notification.LastCandle.Interval))
            {
                return Task.CompletedTask;
            }

            _rsiPatternLookbackPeriods = 60 * 60 * 24 / (int)notification.LastCandle.Interval;

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

        private OrderSide? GetTrendSignal(double[][] smasHigherTimeFrame, double[][] smas, double[][] rsis)
        {
            var fastSmasHigherTimeFrame = smasHigherTimeFrame.Take(4);

            var fastSmas = smas.Take(3);
            var slowSmas = smas.Skip(fastSmas.Count());

            var fastRsis = rsis.Take(4);

            if (fastSmas.Any(sma => sma.Length < 2) || fastRsis.Any(rsi => rsi.Length < 2))
            {
                return null;
            }

            if (fastSmasHigherTimeFrame.TakeLast(3).PickLatestValues().AreOrderedFromFastToSlow()
                && slowSmas.TakeLast(1).AreUptrending()
                && slowSmas.TakeLast(2).PickLatestValues().AreOrderedFromFastToSlow()
                && fastSmas.PickLatestValues().AreOrderedFromFastToSlow()
                && fastSmas.Take(1).Concat(slowSmas.Take(1)).PickLatestValues().AreOrderedFromSlowToFast()
                && rsis.WereOrderedFromSlowToFast(_rsiPatternLookbackPeriods)
                && !fastRsis.PickPenultimateValues().AreOrderedFromFastToSlow()
                && fastRsis.PickLatestValues().AreOrderedFromFastToSlow())
            {
                return OrderSide.Buy;
            }

            if (fastSmasHigherTimeFrame.TakeLast(3).PickLatestValues().AreOrderedFromSlowToFast()
                && slowSmas.TakeLast(1).AreDowntrending()
                && slowSmas.TakeLast(2).PickLatestValues().AreOrderedFromSlowToFast()
                && fastSmas.PickLatestValues().AreOrderedFromSlowToFast()
                && fastSmas.Take(1).Concat(slowSmas.Take(1)).PickLatestValues().AreOrderedFromFastToSlow()
                && rsis.WereOrderedFromFastToSlow(_rsiPatternLookbackPeriods)
                && !fastRsis.PickPenultimateValues().AreOrderedFromSlowToFast()
                && fastRsis.PickLatestValues().AreOrderedFromSlowToFast())
            {
                return OrderSide.Sell;
            }

            return null;
        }
    }
}
