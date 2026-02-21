using CandlestickData.Application.Interfaces;
using CandlestickData.Application.Notifications;
using CandlestickData.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Infrastructure.Sync;

public sealed class RealtimeIngestionWorker(
    IExchangeDataSource exchangeDataSource,
    IServiceScopeFactory scopeFactory,
    SyncConfiguration syncConfig,
    ILogger<RealtimeIngestionWorker> logger)
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting realtime candle ingestion for {SymbolCount} symbols",
            syncConfig.Symbols.Count);

        await exchangeDataSource.SubscribeToKlineUpdatesAsync(
            syncConfig.Symbols,
            syncConfig.TimeFrames,
            async (candlestick, isFinal) =>
            {
                if (!isFinal)
                    return;

                if (!candlestick.IsValid())
                {
                    logger.LogWarning("Invalid candle received: {Symbol}/{TimeFrame} at {OpenTime}",
                        candlestick.Symbol, candlestick.TimeFrame.ToShortString(), candlestick.OpenTime);
                    return;
                }

                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ICandlestickRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                await repo.UpsertManyAsync([candlestick], cancellationToken);

                await publisher.Publish(
                    new CandlePersistedNotification(
                        candlestick.Symbol,
                        candlestick.TimeFrame,
                        candlestick.OpenTime),
                    cancellationToken);

                logger.LogDebug("Persisted closed candle: {Symbol}/{TimeFrame} at {OpenTime}",
                    candlestick.Symbol, candlestick.TimeFrame.ToShortString(), candlestick.OpenTime);
            },
            cancellationToken);
    }
}
