using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backtesting.Mvp;

/// <summary>
/// Loads USD-M perpetual klines from Binance Futures and exposes them as <see cref="IBacktestKlineSource"/>.
/// Pagination uses the same limit as production sync (1500). Configure REST <see cref="Timeout.InfiniteTimeSpan"/>
/// so client-side rate limiting / retries are not cut off by HttpClient.
/// </summary>
public sealed class BinanceUsdFuturesKlineSource : IBacktestKlineSource
{
    public const string BtcUsdtSymbol = "BTCUSDT";

    /// <summary>Approximate lower bound before BTCUSDT perpetual listing; earlier requests simply return from first available bar.</summary>
    public static DateTime DefaultHistoryStartUtc => new(2019, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private const int MaxKlinesPerRequest = 1500;

    private readonly IReadOnlyList<IBinanceKline> _klines;

    private BinanceUsdFuturesKlineSource(IReadOnlyList<IBinanceKline> klines) =>
        _klines = klines;

    public IReadOnlyList<IBinanceKline> GetKlines() => _klines;

    /// <summary>
    /// REST client suitable for historical kline backfills: <see cref="HttpClient.Timeout"/> is infinite so
    /// Binance.Net rate-limit / retry waits are not cut off by the HTTP stack (same intent as host
    /// <c>options.Rest.RequestTimeout = Infinite</c> on DI <c>BinanceOptions</c>).
    /// </summary>
    public static BinanceRestClient CreateRestClientForBacktest()
    {
        var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var restOptions = Options.Create(new BinanceRestOptions());
        return new BinanceRestClient(http, NullLoggerFactory.Instance, restOptions);
    }

    /// <summary>
    /// Close time (UTC) of the last fully closed 1h candle relative to <paramref name="utcNow"/>.
    /// </summary>
    public static DateTime LastCompletedOneHourBarCloseUtc(DateTime utcNow)
    {
        return new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Loads all BTCUSDT USD-M 1h klines from <paramref name="startUtc"/> through the last completed bar before <paramref name="endTimeUtc"/> (Binance <c>endTime</c>).
    /// </summary>
    public static async Task<BinanceUsdFuturesKlineSource> LoadBtcUsdt1HourAsync(
        IBinanceRestClient client,
        DateTime? startUtc = null,
        DateTime? endTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        var start = startUtc ?? DefaultHistoryStartUtc;
        var end = endTimeUtc ?? LastCompletedOneHourBarCloseUtc(DateTime.UtcNow);

        var list = new List<IBinanceKline>(capacity: 4096);
        var cursor = start;

        while (cursor < end)
        {
            var result = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                    BtcUsdtSymbol,
                    KlineInterval.OneHour,
                    cursor,
                    end,
                    MaxKlinesPerRequest,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Binance USD-M GetKlines failed: {result.Error?.Message ?? "unknown error"}");
            }

            var batch = result.Data;
            if (batch is null || batch.Length == 0)
                break;

            foreach (var k in batch)
            {
                if (k.OpenTime >= end)
                    continue;
                list.Add(CopyToMock(k));
            }

            var lastOpen = batch[^1].OpenTime;
            cursor = lastOpen.AddHours(1);

            if (batch.Length < MaxKlinesPerRequest)
                break;
        }

        return new BinanceUsdFuturesKlineSource(list);
    }

    private static MockBinanceKline CopyToMock(IBinanceKline k) =>
        new()
        {
            OpenTime = k.OpenTime,
            CloseTime = k.CloseTime,
            OpenPrice = k.OpenPrice,
            HighPrice = k.HighPrice,
            LowPrice = k.LowPrice,
            ClosePrice = k.ClosePrice,
            Volume = k.Volume,
            QuoteVolume = k.QuoteVolume,
            TradeCount = k.TradeCount,
            TakerBuyBaseVolume = k.TakerBuyBaseVolume,
            TakerBuyQuoteVolume = k.TakerBuyQuoteVolume
        };
}
