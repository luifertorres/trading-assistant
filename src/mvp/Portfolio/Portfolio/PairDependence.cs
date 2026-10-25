namespace Portfolio;

public static class PairDependence
{
    public static double TailCoExceedance(
        IReadOnlyList<UsdtPoint> left,
        IReadOnlyList<UsdtPoint> right,
        double tailFraction = 0.05)
    {
        if (left.Count != right.Count)
            throw new ArgumentException("Series must have the same length.");

        if (left.Count < 2)
            return 0;

        var returnCount = left.Count - 1;
        var worstCount = Math.Max(1, (int)Math.Ceiling(tailFraction * returnCount));
        var leftWorst = WorstReturnDayIndices(left, worstCount);
        var rightWorst = WorstReturnDayIndices(right, worstCount);
        var overlap = leftWorst.Count(i => rightWorst.Contains(i));
        return (double)overlap / leftWorst.Count;
    }

    public static double UnderwaterPearson(IReadOnlyList<UsdtPoint> left, IReadOnlyList<UsdtPoint> right)
    {
        if (left.Count != right.Count)
            throw new ArgumentException("Series must have the same length.");

        var leftUnder = WalkDrawdown.FromPeak(left);
        var rightUnder = WalkDrawdown.FromPeak(right);
        return Pearson(leftUnder.Select(p => p.DrawdownFraction).ToArray(), rightUnder.Select(p => p.DrawdownFraction).ToArray());
    }

    public static double? CoUnderwaterLift(
        IReadOnlyList<UsdtPoint> left,
        IReadOnlyList<UsdtPoint> right,
        double depth = 0.10)
    {
        if (left.Count != right.Count)
            throw new ArgumentException("Series must have the same length.");

        var threshold = -depth;
        var leftUnder = WalkDrawdown.FromPeak(left);
        var rightUnder = WalkDrawdown.FromPeak(right);
        var count = left.Count;
        var leftBelow = 0;
        var rightBelow = 0;
        var bothBelow = 0;

        for (var i = 0; i < count; i++)
        {
            var l = leftUnder[i].DrawdownFraction <= threshold;
            var r = rightUnder[i].DrawdownFraction <= threshold;
            if (l)
                leftBelow++;
            if (r)
                rightBelow++;
            if (l && r)
                bothBelow++;
        }

        if (leftBelow == 0 || rightBelow == 0)
            return null;

        var pLeft = (double)leftBelow / count;
        var pRight = (double)rightBelow / count;
        var pBoth = (double)bothBelow / count;
        return pBoth / (pLeft * pRight);
    }

    public static double ReturnDuringWorstEpisodes(
        IReadOnlyList<UsdtPoint> left,
        IReadOnlyList<UsdtPoint> right,
        int topK = 3)
    {
        if (left.Count != right.Count)
            throw new ArgumentException("Series must have the same length.");

        var episodes = DrawdownEpisodes.Find(left)
            .OrderBy(e => e.Depth)
            .Take(topK)
            .ToList();

        if (episodes.Count == 0)
            return 0;

        var total = 0.0;
        foreach (var episode in episodes)
        {
            var start = episode.PeakIndex;
            var end = episode.RecoveryIndex ?? episode.TroughIndex;
            if (end <= start)
                end = episode.TroughIndex;

            total += SimpleReturn(right, start, end);
        }

        return total / episodes.Count;
    }

    private static HashSet<int> WorstReturnDayIndices(IReadOnlyList<UsdtPoint> series, int worstCount)
    {
        var returns = new List<(int DayIndex, double LogReturn)>(series.Count - 1);
        for (var i = 1; i < series.Count; i++)
        {
            var logReturn = Math.Log((double)series[i].Usdt / series[i - 1].Usdt);
            returns.Add((i, logReturn));
        }

        return returns
            .OrderBy(r => r.LogReturn)
            .ThenBy(r => r.DayIndex)
            .Take(worstCount)
            .Select(r => r.DayIndex)
            .ToHashSet();
    }

    private static double SimpleReturn(IReadOnlyList<UsdtPoint> series, int startIndex, int endIndex)
    {
        if (endIndex <= startIndex)
            return 0;

        return (double)series[endIndex].Usdt / series[startIndex].Usdt - 1;
    }

    private static double Pearson(double[] left, double[] right)
    {
        var count = left.Length;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXX = 0.0;
        var sumYY = 0.0;
        var sumXY = 0.0;

        for (var i = 0; i < count; i++)
        {
            sumX += left[i];
            sumY += right[i];
            sumXX += left[i] * left[i];
            sumYY += right[i] * right[i];
            sumXY += left[i] * right[i];
        }

        var numerator = count * sumXY - sumX * sumY;
        var denomLeft = count * sumXX - sumX * sumX;
        var denomRight = count * sumYY - sumY * sumY;
        var denominator = Math.Sqrt(denomLeft * denomRight);

        if (denominator == 0.0)
            return 0;

        return numerator / denominator;
    }
}
