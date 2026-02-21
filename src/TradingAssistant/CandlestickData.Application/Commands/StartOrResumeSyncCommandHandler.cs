using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Commands;

public sealed class StartOrResumeSyncCommandHandler(
    ISyncJobRepository syncJobRepository,
    ISyncOrchestrator syncOrchestrator)
    : IRequestHandler<StartOrResumeSyncCommand, SyncCommandResponse>
{
    public async Task<SyncCommandResponse> Handle(
        StartOrResumeSyncCommand request,
        CancellationToken cancellationToken)
    {
        if (syncOrchestrator.IsRunning)
        {
            var existingJob = await syncJobRepository.GetLatestAsync(cancellationToken);
            return new SyncCommandResponse(
                existingJob?.Id ?? Guid.Empty,
                "Running",
                "Sync job is already running.");
        }

        var job = SyncJob.Create(DateTime.UtcNow);
        job.Start(DateTime.UtcNow);
        await syncJobRepository.SaveAsync(job, cancellationToken);
        await syncOrchestrator.StartOrResumeAsync(cancellationToken);

        return new SyncCommandResponse(job.Id, job.State.ToString(), "Sync job started.");
    }
}
