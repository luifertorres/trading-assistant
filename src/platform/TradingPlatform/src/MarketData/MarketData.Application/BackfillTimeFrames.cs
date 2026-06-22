using TradingPlatform.Kernel;

namespace MarketData.Application;

public static class BackfillTimeFrames
{
    public static TimeSpan BarDuration(TimeFrameCode timeFrame) =>
        timeFrame.Value switch
        {
            "4H" => TimeSpan.FromHours(4),
            "1D" => TimeSpan.FromDays(1),
            _ => throw new ArgumentException($"Backfill not supported for timeframe '{timeFrame.Value}'.", nameof(timeFrame))
        };

    public static void EnsureSupported(TimeFrameCode timeFrame) => _ = BarDuration(timeFrame);
}
