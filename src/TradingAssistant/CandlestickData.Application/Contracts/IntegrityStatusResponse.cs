namespace CandlestickData.Application.Contracts;

public record IntegrityStatusResponse(IReadOnlyList<SymbolIntegrityDto> Entries);

public record SymbolIntegrityDto(
    string Symbol,
    string TimeFrame,
    string Status,
    string Reason,
    DateTime? GapFromOpenTime,
    DateTime? GapToOpenTime,
    DateTime? LastVerifiedOpenTime,
    DateTime? DetectedAt);
