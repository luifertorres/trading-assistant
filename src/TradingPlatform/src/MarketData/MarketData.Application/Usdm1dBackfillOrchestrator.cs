using Microsoft.Extensions.Logging;
using TradingPlatform.Kernel;

namespace MarketData.Application;

/// <summary>Loads checkpoint, upserts universe into registry, pages klines forward, upserts through <see cref="ICandleSeriesWriter"/>.</summary>
public sealed class Usdm1dBackfillOrchestrator(
    ICandleSeriesWriter candleWriter,
    IUsdM1dBackfillExchange exchange,
    IInstrumentRegistry registry,
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
            checkpoint = new BackfillCheckpointDocumentV2
            {
                RunId = Guid.NewGuid(),
                MarketDatabasePath = dbFullPath,
                Instruments = []
            };
            log.LogInformation("Starting new backfill run {RunId} (no checkpoint or database path mismatch).", checkpoint.RunId);
        }

        var listings = await exchange.ListUsdtPerpetualInstrumentsAsync(cancellationToken).ConfigureAwait(false);
        log.LogInformation("Backfill universe: {Count} USDT perpetual TRADING instruments.", listings.Count);

        var instruments = new List<(InstrumentId Id, BrokerFetchHandle Handle, string ExchangeSymbol)>(listings.Count);
        foreach (var listing in listings)
        {
            var id = await registry.UpsertAsync(listing.Upsert, cancellationToken).ConfigureAwait(false);
            instruments.Add((id, listing.FetchHandle, listing.Upsert.ExchangeSymbol));
        }

        var instrumentIdSet = instruments.Select(i => i.Id.Value).ToHashSet();
        checkpoint.Instruments.RemoveAll(e => !instrumentIdSet.Contains(e.InstrumentId));
        foreach (var (id, _, _) in instruments)
        {
            if (!checkpoint.Instruments.Any(e => e.InstrumentId == id.Value))
                checkpoint.Instruments.Add(new BackfillCheckpointInstrumentEntryV2 { InstrumentId = id.Value });
        }

        if (options.WriteExchangeInfoSnapshot)
        {
            var idMap = instruments.ToDictionary(i => i.ExchangeSymbol, i => i.Id, StringComparer.Ordinal);
            await exchange
                .WriteExchangeInfoSnapshotAsync(dataRoot, checkpoint.RunId, idMap, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var (instrumentId, fetchHandle, exchangeSymbol) in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = checkpoint.Instruments.First(e => e.InstrumentId == instrumentId.Value);
            try
            {
                if (entry.Complete)
                {
                    log.LogInformation("Skip {ExchangeSymbol} (instrument {InstrumentId}, already complete).", exchangeSymbol, instrumentId);
                    continue;
                }

                var series = new SeriesDescriptor(instrumentId, TimeFrameCode.Day1);
                series.Validate();
                EnsureDailySeries(series);

                log.LogInformation("Backfill {ExchangeSymbol} (instrument {InstrumentId}) …", exchangeSymbol, instrumentId);

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
                        .GetDailyKlinesPageAsync(fetchHandle, pageStart, endCap, cancellationToken)
                        .ConfigureAwait(false);

                    if (bars.Count == 0)
                    {
                        MarkComplete(entry);
                        TouchCheckpoint(checkpoint);
                        await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                        log.LogInformation("{ExchangeSymbol}: no more daily rows (empty page). Marked complete.", exchangeSymbol);
                        break;
                    }

                    await candleWriter.UpsertAsync(series, bars, cancellationToken).ConfigureAwait(false);

                    var maxOpen = bars[^1].OpenTime;
                    entry.LastWrittenOpenTimeMs = maxOpen.ToUnixTimeMilliseconds();
                    TouchCheckpoint(checkpoint);
                    await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);

                    if (bars.Count < 1500)
                    {
                        MarkComplete(entry);
                        TouchCheckpoint(checkpoint);
                        await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                        log.LogInformation("{ExchangeSymbol}: caught up (last page had {N} rows). Marked complete.", exchangeSymbol, bars.Count);
                        break;
                    }

                    pageStart = maxOpen.Add(OneDay);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                entry.LastErrorMessage = ex.Message;
                entry.LastErrorAtUtc = DateTimeOffset.UtcNow;
                TouchCheckpoint(checkpoint);
                await checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);

                var displaySymbol = await ResolveDisplaySymbolAsync(instrumentId, exchangeSymbol, cancellationToken)
                    .ConfigureAwait(false);
                log.LogWarning(ex, "{ExchangeSymbol}: backfill failed; recorded error and continuing with next instrument.", displaySymbol);
            }
        }
    }

    private async Task<string> ResolveDisplaySymbolAsync(
        InstrumentId instrumentId,
        string fallbackSymbol,
        CancellationToken cancellationToken)
    {
        var instrument = await registry.GetByIdAsync(instrumentId, cancellationToken).ConfigureAwait(false);
        return instrument?.ExchangeSymbol ?? fallbackSymbol;
    }

    private static void TouchCheckpoint(BackfillCheckpointDocumentV2 checkpoint) =>
        checkpoint.UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void MarkComplete(BackfillCheckpointInstrumentEntryV2 entry)
    {
        entry.Complete = true;
        entry.LastErrorMessage = null;
        entry.LastErrorAtUtc = null;
    }

    internal static void EnsureDailySeries(SeriesDescriptor series)
    {
        if (series.TimeFrame != TimeFrameCode.Day1)
            throw new ArgumentException("Backfill requires daily series (TimeFrameCode.Day1).", nameof(series));
    }
}
