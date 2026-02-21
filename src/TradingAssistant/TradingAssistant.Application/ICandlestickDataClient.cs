namespace TradingAssistant.Application;

public interface ICandlestickDataClient
{
    Task<SyncCommandResult> StartOrResumeSyncAsync(CancellationToken cancellationToken = default);
    Task<IntegrityStatusResult> GetIntegrityStatusAsync(CancellationToken cancellationToken = default);
    Task<CandlesResult> GetCandlesAsync(string symbol, string timeFrame, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task SubscribeToCandleEventsAsync(Func<CandleClosedEvent, Task> onCandleClosed, CancellationToken cancellationToken = default);
}

public record CandleClosedEvent(string Symbol, string TimeFrame, DateTime OpenTime);

public record SyncCommandResult(Guid JobId, string Status, string Message);

public record IntegrityStatusResult(IReadOnlyList<SymbolIntegrityEntry> Entries);

public record SymbolIntegrityEntry(
    string Symbol,
    string TimeFrame,
    string Status,
    string Reason);
