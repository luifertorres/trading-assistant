using TradingPlatform.Kernel;

namespace MarketData.Application;

/// <summary>USD-M REST surface for 1d backfill; implemented in Infrastructure with Binance.Net.</summary>
public interface IUsdM1dBackfillExchange
{
    Task<IReadOnlyList<UsdMInstrumentListing>> ListUsdtPerpetualInstrumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>One page of daily bars, ascending <see cref="OhlcBar.OpenTime"/>, at most 1500 rows.</summary>
    Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
        BrokerFetchHandle handle,
        DateTimeOffset startTimeInclusive,
        DateTimeOffset endTimeInclusive,
        CancellationToken cancellationToken = default);

    /// <summary>One page of bars for the given timeframe, ascending open time, at most 1500 rows.</summary>
    Task<IReadOnlyList<OhlcBar>> GetKlinesPageAsync(
        BrokerFetchHandle handle,
        TimeFrameCode timeFrame,
        DateTimeOffset startTimeInclusive,
        DateTimeOffset endTimeInclusive,
        CancellationToken cancellationToken = default);

    Task WriteExchangeInfoSnapshotAsync(
        string dataRoot,
        Guid runId,
        IReadOnlyDictionary<string, InstrumentId> instrumentIdsByExchangeSymbol,
        CancellationToken cancellationToken = default);
}
