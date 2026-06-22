using Binance.Net.Enums;

namespace TradingAssistant
{
    public record struct CandleId(string Symbol, KlineInterval TimeFrame, DateTime OpenTime);
}
