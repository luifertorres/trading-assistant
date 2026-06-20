using CandlestickData.Application.Interfaces;
using MediatR;

namespace CandlestickData.Application.Notifications;

public sealed class CandlePersistedNotificationHandler(ICandleEventPublisher eventPublisher)
    : INotificationHandler<CandlePersistedNotification>
{
    public async Task Handle(CandlePersistedNotification notification, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishCandleClosedAsync(
            notification.Symbol,
            notification.TimeFrame,
            notification.OpenTime,
            cancellationToken);
    }
}
