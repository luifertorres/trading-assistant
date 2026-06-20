namespace CandlestickData.Application.Contracts;

public record SyncCommandResponse(
    Guid JobId,
    string Status,
    string Message);
