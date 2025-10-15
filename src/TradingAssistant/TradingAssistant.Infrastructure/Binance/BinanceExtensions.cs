using Binance.Net.Enums;

namespace TradingAssistant
{
    public static class BinanceExtensions
    {
        public static OrderSide AsOrderSide(this decimal quantity)
        {
            return quantity >= 0 ? OrderSide.Buy : OrderSide.Sell;
        }

        public static OrderSide Reverse(this OrderSide side)
        {
            return side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
        }

        public static PositionSide AsPositionSide(this OrderSide side)
        {
            return side == OrderSide.Buy ? PositionSide.Long : PositionSide.Short;
        }

        public static decimal WithSide(this decimal quantity, OrderSide side)
        {
            return side is OrderSide.Buy ? quantity : -quantity;
        }
    }
}


