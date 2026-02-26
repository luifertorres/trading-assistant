using MediatR;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure;

namespace TradingAssistant;

public sealed class CandleEventSubscriptionWorker(
    ICandlestickDataClient client,
    ICandleRepository candleRepository,
    IPublisher publisher,
    IConfiguration configuration,
    ILogger<CandleEventSubscriptionWorker> logger) : BackgroundService
{
    private readonly int _candlestickSize = configuration.GetValue<int>("Binance:Service:CandlestickSize", 200);

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

            if (!KlineIntervalExtensions.TryParseFromShortString(evt.TimeFrame, out var klineInterval))
            {
                logger.LogWarning("Unsupported timeframe {TimeFrame} for {Symbol}, skipping", evt.TimeFrame, evt.Symbol);
                return;
            }

            var to = evt.OpenTime;
            var from = to.AddSeconds(-klineInterval.ToSeconds() * (_candlestickSize - 1));
            var candles = await client.GetCandlesAsync(evt.Symbol, evt.TimeFrame, from, to, stoppingToken);

            logger.LogInformation("Fetched {Count} candles for {Symbol}/{TimeFrame} (complete: {IsComplete})",
                candles.Candles.Count, evt.Symbol, evt.TimeFrame, candles.IsComplete);

            var toUpsert = candles.Candles
                .Select(c => (
                    id: new CandleId(c.Symbol, klineInterval, c.OpenTime),
                    candle: new Candle
                    {
                        Symbol = c.Symbol,
                        Interval = klineInterval,
                        OpenTime = c.OpenTime,
                        CloseTime = c.CloseTime,
                        OpenPrice = c.OpenPrice,
                        HighPrice = c.HighPrice,
                        LowPrice = c.LowPrice,
                        ClosePrice = c.ClosePrice
                    }))
                .ToList();

            if (toUpsert.Count > 0)
            {
                candleRepository.UpsertMany(toUpsert);
                var closedCandleId = new CandleId(evt.Symbol, klineInterval, evt.OpenTime);
                await publisher.Publish(new CandleClosedNotification(closedCandleId), stoppingToken);
            }
        }, stoppingToken);
    }
}
