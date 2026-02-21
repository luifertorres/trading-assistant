using TradingAssistant.Application;

namespace TradingAssistant;

public sealed class CandleEventSubscriptionWorker(
    ICandlestickDataClient client,
    ILogger<CandleEventSubscriptionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        logger.LogInformation("Subscribing to candle-closed events from Candlestick Data API...");

        await client.SubscribeToCandleEventsAsync(async evt =>
        {
            logger.LogInformation("Received candle-closed event: {Symbol}/{TimeFrame} at {OpenTime}",
                evt.Symbol, evt.TimeFrame, evt.OpenTime);

            var integrityResult = await client.GetIntegrityStatusAsync(stoppingToken);
            var entry = integrityResult.Entries.FirstOrDefault(
                e => e.Symbol == evt.Symbol && e.TimeFrame == evt.TimeFrame);

            if (entry is not null && entry.Status == "Compromised")
            {
                logger.LogWarning(
                    "Signal inhibited for {Symbol}/{TimeFrame}: CANDLE_INTEGRITY_COMPROMISED (reason: {Reason})",
                    evt.Symbol, evt.TimeFrame, entry.Reason);
                return;
            }

            var to = evt.OpenTime;
            var from = to.AddDays(-30);
            var candles = await client.GetCandlesAsync(evt.Symbol, evt.TimeFrame, from, to, stoppingToken);

            logger.LogInformation("Fetched {Count} candles for {Symbol}/{TimeFrame} (complete: {IsComplete})",
                candles.Candles.Count, evt.Symbol, evt.TimeFrame, candles.IsComplete);

        }, stoppingToken);
    }
}
