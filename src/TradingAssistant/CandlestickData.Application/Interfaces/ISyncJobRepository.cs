using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface ISyncJobRepository
{
    Task<SyncJob?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SyncJob job, CancellationToken cancellationToken = default);
}
