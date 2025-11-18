using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;

namespace TradingAssistant
{
    internal class Rsi5Below10On4hStrategy(IPublisher publisher) : INotificationHandler<SmasAndRsisCalculatedEvent>
    {
        private const KlineInterval FourHours = KlineInterval.FourHour;
        private const int RsiIndexFor5 = 0; // rsiLengths[0] == 5 in RsiCandleClosedHandler
        private const double Threshold = 10.0;

        public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.LastCandle.Interval != FourHours)
            {
                return Task.CompletedTask;
            }

            var rsis = notification.Rsis;
            if (rsis.Length <= RsiIndexFor5 || rsis[RsiIndexFor5].Length < 1)
            {
                return Task.CompletedTask;
            }

            var rsi5 = rsis[RsiIndexFor5][^1];

            if (rsi5 < Threshold)
            {
                var last = notification.LastCandle;
                var tradingSignalNotification = new TradingSignalNotification(last.Symbol,
                    last.Interval,
                    last.OpenTime,
                    PositionSide.Long,
                    OrderSide.Buy,
                    last.ClosePrice);

                return publisher.Publish(tradingSignalNotification, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
