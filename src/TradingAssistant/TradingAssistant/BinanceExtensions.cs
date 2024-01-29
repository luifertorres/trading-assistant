using Binance.Net.Enums;

namespace TradingAssistant
{
    internal static class BinanceExtensions
    {
        internal static OrderSide AsOrderSide(this decimal quantity)
        {
            return quantity >= 0 ? OrderSide.Buy : OrderSide.Sell;
        }

        internal static OrderSide Reverse(this OrderSide side)
        {
            return side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
        }

        internal static PositionSide AsPositionSide(this OrderSide side)
        {
            return side == OrderSide.Buy ? PositionSide.Long : PositionSide.Short;
        }
    }
}
