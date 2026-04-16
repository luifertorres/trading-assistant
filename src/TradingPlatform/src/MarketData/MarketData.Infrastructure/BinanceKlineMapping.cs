using Binance.Net.Interfaces;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

internal static class BinanceKlineMapping
{
    public static OhlcBar ToOhlcBar(IBinanceKline k)
    {
        var open = new DateTimeOffset(DateTime.SpecifyKind(k.OpenTime, DateTimeKind.Utc));
        var close = new DateTimeOffset(DateTime.SpecifyKind(k.CloseTime, DateTimeKind.Utc));
        return new OhlcBar(open, close, k.OpenPrice, k.HighPrice, k.LowPrice, k.ClosePrice, k.Volume);
    }
}
