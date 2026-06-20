using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Queries;

public sealed class GetSyncStatusQueryHandler(
    ISyncJobRepository syncJobRepository,
    ISyncCheckpointRepository checkpointRepository)
    : IRequestHandler<GetSyncStatusQuery, SyncStatusResponse>
{
    public async Task<SyncStatusResponse> Handle(
        GetSyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        var job = await syncJobRepository.GetLatestAsync(cancellationToken);
        var checkpoints = await checkpointRepository.GetAllAsync(cancellationToken);

        var progress = checkpoints.Select(cp => new SymbolSyncProgress(
            cp.Symbol,
            cp.TimeFrame.ToShortString(),
            cp.LastSyncedOpenTime,
            cp.UpdatedAt)).ToList();

        return new SyncStatusResponse(
            job?.Id,
            job?.State.ToString() ?? SyncJobState.Idle.ToString(),
            job?.StartedAt,
            job?.StoppedAt,
            job?.CompletedAt,
            progress);
    }
}
