namespace CandlestickData.Application.Interfaces;

public interface ISyncOrchestrator
{
    Task StartOrResumeAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}
