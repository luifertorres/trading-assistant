using CandlestickData.Domain;

namespace CandlestickData.Infrastructure.Sync;

public sealed class SyncConfiguration
{
    public List<string> Symbols { get; set; } = [];
    public List<TimeFrame> TimeFrames { get; set; } = [TimeFrame.OneMinute, TimeFrame.FiveMinutes, TimeFrame.FifteenMinutes, TimeFrame.OneHour, TimeFrame.OneDay];
    public int BatchSize { get; set; } = 1000;
    public int MaxConcurrentSymbols { get; set; } = 4;
}
