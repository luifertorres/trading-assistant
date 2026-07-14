using Binance.Net.Interfaces;
using WebSocketTrading;

namespace WebSocketTrading.Worker;

internal static class BinanceKlineMapping
{
    internal static Candle ToCandle(IBinanceKline kline) =>
        new()
        {
            Date = kline.OpenTime,
            Open = kline.OpenPrice,
            High = kline.HighPrice,
            Low = kline.LowPrice,
            Close = kline.ClosePrice,
            Volume = kline.Volume
        };

    internal static Candle ToCandle(IBinanceStreamKline kline) =>
        new()
        {
            Date = kline.OpenTime,
            Open = kline.OpenPrice,
            High = kline.HighPrice,
            Low = kline.LowPrice,
            Close = kline.ClosePrice,
            Volume = kline.Volume
        };
}
