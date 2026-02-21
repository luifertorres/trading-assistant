using Binance.Net.Interfaces.Clients;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Infrastructure.Binance;

public sealed class BinanceDataSource(
    IBinanceRestClient restClient,
    IBinanceSocketClient socketClient,
    ILogger<BinanceDataSource> logger) : IExchangeDataSource
{
    private const int MaxKlinesPerRequest = 1500;

    public async Task<IReadOnlyList<CandlestickRecord>> GetKlinesAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime startTime,
        DateTime? endTime,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var interval = timeFrame.ToKlineInterval();
        var effectiveLimit = Math.Min(limit, MaxKlinesPerRequest);

        var result = await restClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
            symbol,
            interval,
            startTime,
            endTime,
            effectiveLimit,
            cancellationToken);

        if (!result.Success)
        {
            logger.LogWarning("Failed to get klines for {Symbol}/{TimeFrame}: {Error}",
                symbol, timeFrame.ToShortString(), result.Error?.Message);
            return [];
        }

        return result.Data
            .Select(k => new CandlestickRecord
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                OpenTime = k.OpenTime,
                CloseTime = k.CloseTime,
                OpenPrice = k.OpenPrice,
                HighPrice = k.HighPrice,
                LowPrice = k.LowPrice,
                ClosePrice = k.ClosePrice,
                Volume = k.Volume
            })
            .ToList();
    }

    public async Task SubscribeToKlineUpdatesAsync(
        IEnumerable<string> symbols,
        IEnumerable<TimeFrame> timeFrames,
        Func<CandlestickRecord, bool, Task> onKlineUpdate,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();
        var intervals = timeFrames.Select(tf => tf.ToKlineInterval()).ToList();

        foreach (var symbol in symbolList)
        {
            foreach (var interval in intervals)
            {
                var result = await socketClient.UsdFuturesApi.ExchangeData
                    .SubscribeToKlineUpdatesAsync(
                        symbol,
                        interval,
                        async data =>
                        {
                            var kline = data.Data.Data;
                            var record = new CandlestickRecord
                            {
                                Symbol = data.Data.Symbol,
                                TimeFrame = interval.ToTimeFrame(),
                                OpenTime = kline.OpenTime,
                                CloseTime = kline.CloseTime,
                                OpenPrice = kline.OpenPrice,
                                HighPrice = kline.HighPrice,
                                LowPrice = kline.LowPrice,
                                ClosePrice = kline.ClosePrice,
                                Volume = kline.Volume
                            };

                            await onKlineUpdate(record, kline.Final);
                        },
                        ct: cancellationToken);

                if (!result.Success)
                {
                    logger.LogError("Failed to subscribe to kline updates for {Symbol}/{Interval}: {Error}",
                        symbol, interval, result.Error?.Message);
                }
            }
        }
    }
}
