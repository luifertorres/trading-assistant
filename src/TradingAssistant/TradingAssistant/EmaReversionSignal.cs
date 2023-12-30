using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class EmaReversionSignal : INotification
    {
        public required DateTime Time { get; init; }
        public required KlineInterval TimeFrame { get; init; }
        public required PositionSide Direction { get; init; }
        public required OrderSide Side { get; init; }
        public required string Symbol { get; init; }
        public required decimal EntryPrice { get; init; }
    }
}
