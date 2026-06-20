namespace CandlestickData.Application.Contracts;

public record SyncStatusResponse(
    Guid? JobId,
    string State,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    DateTime? CompletedAt,
    IReadOnlyList<SymbolSyncProgress> SymbolProgress);

public record SymbolSyncProgress(
    string Symbol,
    string TimeFrame,
    DateTime? LastSyncedOpenTime,
    DateTime? UpdatedAt);
