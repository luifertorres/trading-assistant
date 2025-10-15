using Binance.Net.Enums;

namespace TradingAssistant;

public readonly record struct CandleId(string Symbol, KlineInterval TimeFrame, DateTime OpenTime);


