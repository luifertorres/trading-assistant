namespace Portfolio;

public readonly record struct CorrelationPoint(DateTimeOffset Time, double Correlation);

public static class SeriesCorrelation
{
    public const int DefaultWindow = 30;

    public static IReadOnlyList<CorrelationPoint> RollingLogReturnPearson(
        IReadOnlyList<UsdtPoint> left,
        IReadOnlyList<UsdtPoint> right,
        int window = DefaultWindow)
    {
        if (window < 1)
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be at least 1.");

        if (left.Count != right.Count)
            throw new ArgumentException("Series must have the same length.");

        if (left.Count < window + 1)
            return [];

        ValidateSeries(left);
        ValidateSeries(right);
        ValidateTimestamps(left, right);

        var points = new List<CorrelationPoint>();

        for (var endIdx = window; endIdx < left.Count; endIdx++)
        {
            var correlation = PearsonLogReturns(left, right, endIdx - window + 1, endIdx);
            if (correlation is null)
                continue;

            points.Add(new CorrelationPoint(left[endIdx].Time, correlation.Value));
        }

        return points;
    }

    private static void ValidateSeries(IReadOnlyList<UsdtPoint> series)
    {
        for (var i = 0; i < series.Count; i++)
        {
            if (series[i].Usdt <= 0)
                throw new ArgumentException("Series must contain only positive USDT values.");
        }
    }

    private static void ValidateTimestamps(IReadOnlyList<UsdtPoint> left, IReadOnlyList<UsdtPoint> right)
    {
        for (var i = 0; i < left.Count; i++)
        {
            if (left[i].Time != right[i].Time)
                throw new ArgumentException("Series must have matching timestamps.");
        }
    }

    private static double? PearsonLogReturns(
        IReadOnlyList<UsdtPoint> left,
        IReadOnlyList<UsdtPoint> right,
        int startReturnIdx,
        int endReturnIdx)
    {
        var count = endReturnIdx - startReturnIdx + 1;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXX = 0.0;
        var sumYY = 0.0;
        var sumXY = 0.0;

        for (var i = startReturnIdx; i <= endReturnIdx; i++)
        {
            var x = LogReturn(left[i - 1].Usdt, left[i].Usdt);
            var y = LogReturn(right[i - 1].Usdt, right[i].Usdt);
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumYY += y * y;
            sumXY += x * y;
        }

        var numerator = count * sumXY - sumX * sumY;
        var denomLeft = count * sumXX - sumX * sumX;
        var denomRight = count * sumYY - sumY * sumY;
        var denominator = Math.Sqrt(denomLeft * denomRight);

        if (denominator == 0.0)
            return null;

        return numerator / denominator;
    }

    private static double LogReturn(int previousUsdt, int currentUsdt) =>
        Math.Log((double)currentUsdt / previousUsdt);
}
