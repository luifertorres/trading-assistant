namespace CandlestickData.Domain;

public class SyncCheckpoint
{
    public required string Symbol { get; init; }
    public required TimeFrame TimeFrame { get; init; }
    public DateTime LastSyncedOpenTime { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Advance(DateTime openTime, DateTime now)
    {
        if (openTime <= LastSyncedOpenTime)
            return;

        LastSyncedOpenTime = openTime;
        UpdatedAt = now;
    }

    public static SyncCheckpoint Create(string symbol, TimeFrame timeFrame, DateTime lastSyncedOpenTime, DateTime now) => new()
    {
        Symbol = symbol,
        TimeFrame = timeFrame,
        LastSyncedOpenTime = lastSyncedOpenTime,
        UpdatedAt = now
    };
}
