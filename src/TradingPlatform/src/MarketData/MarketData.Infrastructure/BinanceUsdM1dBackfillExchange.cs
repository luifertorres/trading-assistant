using System.Text.Json;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Enums;
using MarketData.Application;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

/// <summary>USD-M REST adapter; rate limits and retries are handled by Binance.Net.</summary>
public sealed class BinanceUsdM1dBackfillExchange(IBinanceRestClient rest) : IUsdM1dBackfillExchange
{
    private const int MaxKlinesPerRequest = 1500;

    public async Task<IReadOnlyList<string>> GetActiveUsdtPerpetualSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var result = await rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {result.Error?.Message}");

        return result.Data.Symbols
            .Where(s => s.Status == SymbolStatus.Trading)
            .Where(s => s.ContractType == ContractType.Perpetual)
            .Where(s => string.Equals(s.QuoteAsset, "USDT", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
        string symbol,
        DateTimeOffset startTimeInclusive,
        DateTimeOffset endTimeInclusive,
        CancellationToken cancellationToken = default)
    {
        var start = startTimeInclusive.UtcDateTime;
        var end = endTimeInclusive.UtcDateTime;

        var result = await rest.UsdFuturesApi.ExchangeData.GetKlinesAsync(
            symbol,
            KlineInterval.OneDay,
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

    public async Task WriteExchangeInfoSnapshotAsync(string dataRoot, Guid runId, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataRoot);
        var result = await rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {result.Error?.Message}");

        var path = Path.Combine(dataRoot, $"exchangeInfo-usdm-snapshot-{runId:N}.json");
        var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
