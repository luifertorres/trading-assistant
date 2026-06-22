using System.Text.Json;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using MarketData.Application;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

/// <summary>USD-M REST adapter; rate limits and retries are handled by Binance.Net.</summary>
public sealed class BinanceUsdM1dBackfillExchange(IBinanceRestClient rest) : IUsdM1dBackfillExchange
{
    private const int MaxKlinesPerRequest = 1500;
    private const string Venue = "binance";
    private const string Market = "usdm";
    private const string RegistryContractType = "perpetual";

    public async Task<IReadOnlyList<UsdMInstrumentListing>> ListUsdtPerpetualInstrumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {result.Error?.Message}");

        var seenAt = DateTimeOffset.UtcNow;
        return result.Data.Symbols
            .Where(s => s.Status == SymbolStatus.Trading)
            .Where(s => s.ContractType == ContractType.Perpetual)
            .Where(s => string.Equals(s.QuoteAsset, "USDT", StringComparison.OrdinalIgnoreCase))
            .Select(s => new UsdMInstrumentListing(
                new InstrumentUpsert(
                    Venue,
                    Market,
                    RegistryContractType,
                    s.Name,
                    s.BaseAsset,
                    s.QuoteAsset,
                    s.Pair,
                    s.PricePrecision,
                    s.QuantityPrecision,
                    JsonSerializer.Serialize(s.Filters),
                    s.Status.ToString(),
                    seenAt),
                new BrokerFetchHandle(s.Name)))
            .OrderBy(l => l.Upsert.ExchangeSymbol, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
        BrokerFetchHandle handle,
        DateTimeOffset startTimeInclusive,
        DateTimeOffset endTimeInclusive,
        CancellationToken cancellationToken = default) =>
        await GetKlinesPageAsync(handle, TimeFrameCode.Day1, startTimeInclusive, endTimeInclusive, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<OhlcBar>> GetKlinesPageAsync(
        BrokerFetchHandle handle,
        TimeFrameCode timeFrame,
        DateTimeOffset startTimeInclusive,
        DateTimeOffset endTimeInclusive,
        CancellationToken cancellationToken = default)
    {
        var symbol = BrokerFetchHandleUnwrap.Symbol(handle);
        var interval = BackfillKlineIntervalMapping.ToBinance(timeFrame);
        var start = startTimeInclusive.UtcDateTime;
        var end = endTimeInclusive.UtcDateTime;

        var result = await rest.UsdFuturesApi.ExchangeData.GetKlinesAsync(
            symbol,
            interval,
            start,
            end,
            MaxKlinesPerRequest,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Get klines failed for {symbol}: {result.Error?.Message}");

        return result.Data
            .Select(BinanceKlineMapping.ToOhlcBar)
            .OrderBy(b => b.OpenTime)
            .ToList();
    }

    public async Task WriteExchangeInfoSnapshotAsync(
        string dataRoot,
        Guid runId,
        IReadOnlyDictionary<string, InstrumentId> instrumentIdsByExchangeSymbol,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataRoot);
        var result = await rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {result.Error?.Message}");

        var exchangeInfoPath = Path.Combine(dataRoot, $"exchangeInfo-usdm-snapshot-{runId:N}.json");
        var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(exchangeInfoPath, json, cancellationToken).ConfigureAwait(false);

        var idMap = instrumentIdsByExchangeSymbol.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Value,
            StringComparer.Ordinal);
        var idMapPath = Path.Combine(dataRoot, $"instrument-ids-{runId:N}.json");
        var idMapJson = JsonSerializer.Serialize(idMap, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(idMapPath, idMapJson, cancellationToken).ConfigureAwait(false);
    }
}
