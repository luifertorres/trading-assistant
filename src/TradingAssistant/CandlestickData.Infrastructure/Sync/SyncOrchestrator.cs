using CandlestickData.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Infrastructure.Sync;

public sealed class SyncOrchestrator(
    HistoricalSyncWorker historicalSyncWorker,
    RealtimeIngestionWorker realtimeIngestionWorker,
    ISyncJobRepository syncJobRepository,
    ILogger<SyncOrchestrator> logger) : ISyncOrchestrator
{
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public async Task StartOrResumeAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            logger.LogInformation("Sync is already running");
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _runningTask = Task.Run(async () =>
        {
            try
            {
                var historicalTask = historicalSyncWorker.SyncAsync(_cts.Token);
                var realtimeTask = realtimeIngestionWorker.StartAsync(_cts.Token);
                await Task.WhenAll(historicalTask, realtimeTask);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Sync orchestrator cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync orchestrator encountered an error");

                var job = await syncJobRepository.GetLatestAsync(CancellationToken.None);
                if (job is { IsRunning: true })
                {
                    job.Fail(ex.Message, DateTime.UtcNow);
                    await syncJobRepository.SaveAsync(job, CancellationToken.None);
                }
            }
        }, _cts.Token);

        logger.LogInformation("Sync orchestrator started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();

            if (_runningTask is not null)
            {
                try
                {
                    await _runningTask.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (TimeoutException)
                {
                    logger.LogWarning("Sync orchestrator stop timed out");
                }
            }

            _cts.Dispose();
            _cts = null;
        }

        logger.LogInformation("Sync orchestrator stopped");
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartOrResumeAsync(cancellationToken);
    }
}
