using Binance.Net.Enums;

namespace TradingAssistant
{
    public record struct CandlestickId(string Symbol, KlineInterval TimeFrame);
}
