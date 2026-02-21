using CandlestickData.Domain;

namespace CandlestickData.Application.Contracts;

public record CandlesResponse(
    IReadOnlyList<CandlestickDto> Candles,
    bool IsComplete,
    DateTime? FromOpenTime,
    DateTime? ToOpenTime,
    IReadOnlyList<MissingRange> MissingRanges);

public record CandlestickDto(
    string Symbol,
    string TimeFrame,
    DateTime OpenTime,
    DateTime CloseTime,
    decimal OpenPrice,
    decimal HighPrice,
    decimal LowPrice,
    decimal ClosePrice,
    decimal Volume);

public record MissingRange(DateTime From, DateTime To);
