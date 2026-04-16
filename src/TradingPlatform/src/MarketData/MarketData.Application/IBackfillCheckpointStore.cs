namespace MarketData.Application;

public interface IBackfillCheckpointStore
{
    Task<BackfillCheckpointDocumentV1?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BackfillCheckpointDocumentV1 document, CancellationToken cancellationToken = default);
}
