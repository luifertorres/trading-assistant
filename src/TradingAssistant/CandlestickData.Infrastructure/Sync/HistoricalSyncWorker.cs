using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Infrastructure.Sync;

public sealed class HistoricalSyncWorker(
    IServiceScopeFactory scopeFactory,
    IExchangeDataSource exchangeDataSource,
    SyncConfiguration syncConfig,
    ILogger<HistoricalSyncWorker> logger)
{
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting historical sync for {SymbolCount} symbols, {TimeFrameCount} timeframes",
            syncConfig.Symbols.Count, syncConfig.TimeFrames.Count);

        var semaphore = new SemaphoreSlim(syncConfig.MaxConcurrentSymbols);

        var tasks = syncConfig.Symbols.SelectMany(symbol =>
            syncConfig.TimeFrames.Select(tf => SyncSymbolTimeFrameAsync(
                symbol, tf, semaphore, cancellationToken)));

        await Task.WhenAll(tasks);

        logger.LogInformation("Historical sync completed");
    }

    private async Task SyncSymbolTimeFrameAsync(
        string symbol,
        TimeFrame timeFrame,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var checkpointRepo = scope.ServiceProvider.GetRequiredService<ISyncCheckpointRepository>();
            var candlestickRepo = scope.ServiceProvider.GetRequiredService<ICandlestickRepository>();

            var checkpoint = await checkpointRepo.GetAsync(symbol, timeFrame, cancellationToken);
            var startTime = checkpoint?.LastSyncedOpenTime.Add(timeFrame.ToTimeSpan()) ?? DateTime.UtcNow.AddYears(-1);

            logger.LogInformation("Syncing {Symbol}/{TimeFrame} from {StartTime}",
                symbol, timeFrame.ToShortString(), startTime);

            while (startTime < DateTime.UtcNow && !cancellationToken.IsCancellationRequested)
            {
                var klines = await exchangeDataSource.GetKlinesAsync(
                    symbol, timeFrame, startTime, null, syncConfig.BatchSize, cancellationToken);

                if (klines.Count == 0)
                    break;

                var validCandles = klines.Where(c => c.IsValid()).ToList();
                if (validCandles.Count > 0)
                {
                    await candlestickRepo.UpsertManyAsync(validCandles, cancellationToken);

                    var lastOpenTime = validCandles[^1].OpenTime;
                    var now = DateTime.UtcNow;

                    if (checkpoint is null)
                    {
                        checkpoint = SyncCheckpoint.Create(symbol, timeFrame, lastOpenTime, now);
                    }
                    else
                    {
                        checkpoint.Advance(lastOpenTime, now);
                    }

                    await checkpointRepo.SaveAsync(checkpoint, cancellationToken);

                    startTime = lastOpenTime + timeFrame.ToTimeSpan();
                }

                if (klines.Count < syncConfig.BatchSize)
                    break;
            }

            logger.LogInformation("Sync complete for {Symbol}/{TimeFrame}", symbol, timeFrame.ToShortString());
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Sync cancelled for {Symbol}/{TimeFrame}", symbol, timeFrame.ToShortString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed for {Symbol}/{TimeFrame}", symbol, timeFrame.ToShortString());
        }
        finally
        {
            semaphore.Release();
        }
    }
}
