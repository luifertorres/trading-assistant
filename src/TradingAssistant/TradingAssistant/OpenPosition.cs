using Binance.Net.Enums;

namespace TradingAssistant
{
    public class OpenPosition
    {
        public Guid Id { get; set; }
        public required string Symbol { get; init; }
        public required int Leverage { get; set; }
        public required PositionSide PositionSide { get; init; }
        public OrderSide EntrySide => Quantity.AsOrderSide();
        public required decimal EntryPrice { get; set; }
        public required decimal Quantity { get; set; }
        public decimal Notional => EntryPrice * Quantity;
        public required decimal BreakEvenPrice { get; set; }
        public bool HasStopLossInBreakEven { get; set; }
        public required DateTimeOffset UpdateTime { get; set; }
    }
}
