using CandlestickData.Domain;

namespace CandlestickData.Application.Contracts;

public record CandlesRequest(
    IReadOnlyList<string> Symbols,
    TimeFrame TimeFrame,
    DateTime From,
    DateTime To);
