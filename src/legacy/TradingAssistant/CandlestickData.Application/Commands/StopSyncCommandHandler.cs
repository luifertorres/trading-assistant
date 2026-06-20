using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using MediatR;

namespace CandlestickData.Application.Commands;

public sealed class StopSyncCommandHandler(
    ISyncJobRepository syncJobRepository,
    ISyncOrchestrator syncOrchestrator)
    : IRequestHandler<StopSyncCommand, SyncCommandResponse>
{
    public async Task<SyncCommandResponse> Handle(
        StopSyncCommand request,
        CancellationToken cancellationToken)
    {
        var job = await syncJobRepository.GetLatestAsync(cancellationToken);

        if (job is null || !job.IsRunning)
        {
            return new SyncCommandResponse(
                job?.Id ?? Guid.Empty,
                job?.State.ToString() ?? "Idle",
                "No running sync job to stop.");
        }

        await syncOrchestrator.StopAsync(cancellationToken);
        job.Stop(DateTime.UtcNow);
        await syncJobRepository.SaveAsync(job, cancellationToken);

        return new SyncCommandResponse(job.Id, job.State.ToString(), "Sync job stopped.");
    }
}
