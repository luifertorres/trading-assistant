namespace TradingAssistant.Application;

public record CandlesResult(
    IReadOnlyList<CandleDto> Candles,
    bool IsComplete,
    DateTime? FromOpenTime = null,
    DateTime? ToOpenTime = null,
    IReadOnlyList<MissingRangeDto>? MissingRanges = null);

public record MissingRangeDto(DateTime From, DateTime To);

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
