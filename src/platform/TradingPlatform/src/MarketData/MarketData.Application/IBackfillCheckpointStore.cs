namespace MarketData.Application;

public interface IBackfillCheckpointStore
{
    Task<BackfillCheckpointDocumentV2?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BackfillCheckpointDocumentV2 document, CancellationToken cancellationToken = default);
}
