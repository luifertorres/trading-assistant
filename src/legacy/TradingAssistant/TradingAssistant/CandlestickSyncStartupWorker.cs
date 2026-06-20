using TradingAssistant.Application;

namespace TradingAssistant;

public sealed class CandlestickSyncStartupWorker(
    ICandlestickDataClient client,
    ILogger<CandlestickSyncStartupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        logger.LogInformation("Triggering candlestick data sync...");

        var result = await client.StartOrResumeSyncAsync(stoppingToken);

        logger.LogInformation("Candlestick sync response: {Status} - {Message} (JobId: {JobId})",
            result.Status, result.Message, result.JobId);
    }
}
