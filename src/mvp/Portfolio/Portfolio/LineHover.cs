namespace Portfolio;

public static class LineHover
{
    public static UsdtPoint NearestByX(IReadOnlyList<UsdtPoint> series, DateTimeOffset x)
    {
        var nearest = series[0];
        var best = Math.Abs((series[0].Time - x).Ticks);

        foreach (var point in series)
        {
            var distance = Math.Abs((point.Time - x).Ticks);
            if (distance < best)
            {
                best = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    public static string Format(UsdtPoint point)
    {
        var utcMinusFive = point.Time.ToOffset(UtcMinusFiveUsdtSeries.Offset);
        return $"{utcMinusFive:yyyy-MM-dd HH:mm} UTC-5 | {point.Usdt} USDT";
    }
}
