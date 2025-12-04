using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public record TradeRequest(string Symbol,
        KlineInterval TimeFrame,
        DateTime Time,
        PositionSide Direction,
        OrderSide Side,
        decimal EntryPrice,
        decimal? MarginPercentage = null,
        bool IsPyramidingAllowed = false,
        bool IsStopLossDisabled = false) : IRequest<bool>;
}
