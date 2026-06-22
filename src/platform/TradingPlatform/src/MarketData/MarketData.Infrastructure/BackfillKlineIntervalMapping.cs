using Binance.Net.Enums;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

internal static class BackfillKlineIntervalMapping
{
    public static KlineInterval ToBinance(TimeFrameCode timeFrame) =>
        timeFrame.Value switch
        {
            "4H" => KlineInterval.FourHour,
            "1D" => KlineInterval.OneDay,
            _ => throw new ArgumentException($"Backfill not supported for timeframe '{timeFrame.Value}'.", nameof(timeFrame))
        };
}
