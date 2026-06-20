using Binance.Net.Enums;

namespace TradingAssistant;

public struct Candle
{
    public required string Symbol { get; set; }
    public required KlineInterval Interval { get; set; }
    public required DateTime OpenTime { get; set; }
    public required DateTime CloseTime { get; set; }
    public required decimal OpenPrice { get; set; }
    public required decimal HighPrice { get; set; }
    public required decimal LowPrice { get; set; }
    public required decimal ClosePrice { get; set; }
}


