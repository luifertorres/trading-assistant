namespace CandlestickData.Domain;

public static class GapDetector
{
    public static IReadOnlyList<DetectedGap> DetectGaps(
        IReadOnlyList<CandlestickRecord> candles,
        TimeFrame timeFrame)
    {
        if (candles.Count < 2)
            return [];

        var expectedInterval = timeFrame.ToTimeSpan();
        var gaps = new List<DetectedGap>();

        for (var i = 1; i < candles.Count; i++)
        {
            var expected = candles[i - 1].OpenTime + expectedInterval;
            var actual = candles[i].OpenTime;

            if (actual > expected)
            {
                gaps.Add(new DetectedGap(
                    candles[i - 1].OpenTime,
                    actual,
                    (int)((actual - expected) / expectedInterval)));
            }
        }

        return gaps;
    }

    public static GapClassification Classify(DetectedGap gap, IReadOnlySet<DateTime>? knownMaintenanceWindows = null)
    {
        if (knownMaintenanceWindows is not null)
        {
            var checkTime = gap.FromOpenTime;
            while (checkTime < gap.ToOpenTime)
            {
                if (knownMaintenanceWindows.Contains(checkTime.Date))
                    return GapClassification.Normal;

                checkTime = checkTime.AddDays(1);
            }
        }

        return GapClassification.Atypical;
    }
}

public record DetectedGap(DateTime FromOpenTime, DateTime ToOpenTime, int MissingCandleCount);
