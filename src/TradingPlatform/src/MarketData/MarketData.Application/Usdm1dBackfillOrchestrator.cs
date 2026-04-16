using MarketData.Domain;
using Microsoft.Extensions.Logging;
using TradingPlatform.Kernel;

namespace MarketData.Application;

/// <summary>Loads checkpoint, lists symbols, pages klines forward, upserts through <see cref="ICandleSeriesWriter"/>. Exchange pacing is delegated to Binance.Net.</summary>
public sealed class Usdm1dBackfillOrchestrator(
    ICandleSeriesWriter candleWriter,
    IUsdM1dBackfillExchange exchange,
    IBackfillCheckpointStore checkpointStore,
    ILogger<Usdm1dBackfillOrchestrator> log)
{
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    public async Task RunAsync(Usdm1dBackfillRunOptions options, CancellationToken cancellationToken = default)
    {
        var dbFullPath = Path.GetFullPath(options.MarketDatabasePath);
        var dataRoot = options.DataRoot;
        Directory.CreateDirectory(dataRoot);

        var checkpoint = await checkpointStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (checkpoint is null || !string.Equals(Path.GetFullPath(checkpoint.MarketDatabasePath), dbFullPath, StringComparison.OrdinalIgnoreCase))
        {
            checkpoint = new BackfillCheckpointDocumentV1
            {
                RunId = Guid.NewGuid(),
                MarketDatabasePath = dbFullPath,
                Symbols = []
            };
            log.LogInformation("Starting new backfill run {RunId} (no checkpoint or database path mismatch).", checkpoint.RunId);
        }

        if (options.WriteExchangeInfoSnapshot)
        {
            await exchange
                .WriteExchangeInfoSnapshotAsync(dataRoot, checkpoint.RunId, cancellationToken)
                .ConfigureAwait(false);
        }

        var symbols = await exchange.GetActiveUsdtPerpetualSymbolsAsync(cancellationToken).ConfigureAwait(false);
        log.LogInformation("Backfill universe: {Count} USDT perpetual TRADING symbols.", symbols.Count);

        var symbolSet = symbols.ToHashSet(StringComparer.Ordinal);
        checkpoint.Symbols.RemoveAll(e => !symbolSet.Contains(e.Symbol));

        foreach (var sym in symbols)
        {
            if (!checkpoint.Symbols.Any(e => e.Symbol == sym))
                checkpoint.Symbols.Add(new BackfillCheckpointSymbolEntryV1 { Symbol = sym });
        }

        foreach (var symbol in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = checkpoint.Symbols.First(e => e.Symbol == symbol);
            if (entry.Complete)
            {
                log.LogInformation("Skip {Symbol} (already complete in checkpoint).", symbol);
                continue;
            }

            var series = new SeriesDescriptor(symbol, TimeFrameCode.Day1);
            series.Validate();
            EnsureDailySeries(series);

            var tableName = SeriesTableNaming.ToPhysicalTableName(series);
            log.LogInformation("Backfill {Symbol} → table {Table} …", symbol, tableName);

            DateTimeOffset pageStart;
            if (entry.LastWrittenOpenTimeMs is { } ms)
            {
                var lastOpen = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                pageStart = lastOpen.Add(OneDay);
            }
            else
            {
                pageStart = DateTimeOffset.FromUnixTimeMilliseconds(0);
            }

            var endCap = DateTimeOffset.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                var bars = await exchange
                    .GetDailyKlinesPageAsync(symbol, pageStart, endCap, cancellationToken)
                    .ConfigureAwait(false);

                if (bars.Count == 0)
                {
                    entry.Complete = true;
                    TouchCheckpoint(checkpoint);
                    await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                    log.LogInformation("{Symbol}: no more daily rows (empty page). Marked complete.", symbol);
                    break;
                }

                await candleWriter.UpsertAsync(series, bars, cancellationToken).ConfigureAwait(false);

                var maxOpen = bars[^1].OpenTime;
                entry.LastWrittenOpenTimeMs = maxOpen.ToUnixTimeMilliseconds();
                TouchCheckpoint(checkpoint);
                await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);

                if (bars.Count < 1500)
                {
                    entry.Complete = true;
                    TouchCheckpoint(checkpoint);
                    await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                    log.LogInformation("{Symbol}: caught up (last page had {N} rows). Marked complete.", symbol, bars.Count);
                    break;
                }

                pageStart = maxOpen.Add(OneDay);
            }
        }
    }

    private static void TouchCheckpoint(BackfillCheckpointDocumentV1 checkpoint) =>
        checkpoint.UpdatedAtUtc = DateTimeOffset.UtcNow;

    internal static void EnsureDailySeries(SeriesDescriptor series)
    {
        if (series.TimeFrame != TimeFrameCode.Day1)
            throw new ArgumentException("Backfill requires daily series (TimeFrameCode.Day1).", nameof(series));
    }
}
