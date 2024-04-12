using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public record TradingSignalNotification(string Symbol,
        KlineInterval TimeFrame,
        DateTime Time,
        PositionSide Direction,
        OrderSide Side,
        decimal EntryPrice) : INotification;
}
