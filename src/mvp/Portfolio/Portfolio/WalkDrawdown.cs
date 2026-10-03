namespace Portfolio;

public readonly record struct DrawdownPoint(DateTimeOffset Time, double DrawdownFraction);

public static class WalkDrawdown
{
    public static IReadOnlyList<DrawdownPoint> FromPeak(IReadOnlyList<UsdtPoint> series)
    {
        if (series.Count == 0)
            return [];

        var points = new List<DrawdownPoint>(series.Count);
        var peak = 0;

        foreach (var point in series)
        {
            if (point.Usdt <= 0)
                throw new ArgumentException("Series must contain only positive USDT values.", nameof(series));

            if (point.Usdt > peak)
                peak = point.Usdt;

            var drawdown = (double)point.Usdt / peak - 1;
            points.Add(new DrawdownPoint(point.Time, drawdown));
        }

        return points;
    }
}
