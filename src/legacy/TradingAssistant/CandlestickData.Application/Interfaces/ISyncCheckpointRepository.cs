using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface ISyncCheckpointRepository
{
    Task<SyncCheckpoint?> GetAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyncCheckpoint>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}
