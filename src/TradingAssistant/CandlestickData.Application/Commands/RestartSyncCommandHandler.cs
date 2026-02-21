using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Commands;

public sealed class RestartSyncCommandHandler(
    ISyncJobRepository syncJobRepository,
    ISyncOrchestrator syncOrchestrator)
    : IRequestHandler<RestartSyncCommand, SyncCommandResponse>
{
    public async Task<SyncCommandResponse> Handle(
        RestartSyncCommand request,
        CancellationToken cancellationToken)
    {
        var existingJob = await syncJobRepository.GetLatestAsync(cancellationToken);

        if (existingJob is { IsRunning: true })
        {
            existingJob.Stop(DateTime.UtcNow);
            await syncJobRepository.SaveAsync(existingJob, cancellationToken);
        }

        await syncOrchestrator.RestartAsync(cancellationToken);

        var job = SyncJob.Create(DateTime.UtcNow);
        job.Start(DateTime.UtcNow);
        await syncJobRepository.SaveAsync(job, cancellationToken);

        return new SyncCommandResponse(job.Id, job.State.ToString(), "Sync job restarted.");
    }
}
