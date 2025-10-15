using Binance.Net.Enums;

namespace TradingAssistant;

public readonly record struct CandlestickId(string Symbol, KlineInterval TimeFrame);


