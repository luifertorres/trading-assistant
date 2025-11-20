using MediatR;
using TradingAssistant.Application;

namespace TradingAssistant;

internal sealed class RsiIndicatorConditionDispatcher(IPublisher publisher)
    : INotificationHandler<SmasAndRsisCalculatedEvent>
{
    private const int RsiIndexFor5 = 0; // rsiLengths[0] == 5 in RsiCandleClosedHandler
    private const double OversoldThreshold = 10.0;

    public Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
    {
        var rsi5 = TryGetLatestRsi(notification.Rsis, RsiIndexFor5);

        if (rsi5 is null)
        {
            return Task.CompletedTask;
        }

        if (rsi5 <= OversoldThreshold)
        {
            var last = notification.LastCandle;

            if (SymbolExclusions.Contains(last.Symbol))
            {
                return Task.CompletedTask;
            }

            var conditionEvent = new IndicatorConditionMetNotification(last.Symbol,
                last.Interval,
                last.OpenTime,
                last.ClosePrice,
                IndicatorType.Rsi,
                5,
                rsi5.Value,
                OversoldThreshold,
                IndicatorComparison.LessThanOrEqual,
                IndicatorEventSource.CandleClose);

            return publisher.Publish(conditionEvent, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static double? TryGetLatestRsi(double[][] rsis, int index)
    {
        if (rsis.Length <= index)
        {
            return null;
        }

        var series = rsis[index];

        if (series.Length == 0)
        {
            return null;
        }

        return series[^1];
    }
}


