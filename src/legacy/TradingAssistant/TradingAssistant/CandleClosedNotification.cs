using MediatR;

namespace TradingAssistant
{
    public record CandleClosedNotification(CandleId CandleId) : INotification;
}
