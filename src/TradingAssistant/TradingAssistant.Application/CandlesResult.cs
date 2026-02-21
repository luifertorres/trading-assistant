namespace TradingAssistant.Application;

public record CandlesResult(
    IReadOnlyList<CandleDto> Candles,
    bool IsComplete);

public record CandleDto(
    string Symbol,
    string TimeFrame,
    DateTime OpenTime,
    DateTime CloseTime,
    decimal OpenPrice,
    decimal HighPrice,
    decimal LowPrice,
    decimal ClosePrice,
    decimal Volume);
