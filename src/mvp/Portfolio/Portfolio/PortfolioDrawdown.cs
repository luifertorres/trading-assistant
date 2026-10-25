namespace Portfolio;

public readonly record struct DrawdownProfile(
    double MaxDrawdown,
    double UlcerIndex,
    double Cdar95,
    double LongestUnderwaterDays);

public static class PortfolioDrawdown
{
    public static IReadOnlyList<UsdtPoint> EqualWeightIndex(
        IReadOnlyList<IReadOnlyList<UsdtPoint>> walks,
        int startUsdt = 10_000)
    {
        if (walks.Count == 0)
            throw new ArgumentException("At least one walk is required.", nameof(walks));

        var count = walks[0].Count;
        foreach (var walk in walks)
        {
            if (walk.Count != count)
                throw new ArgumentException("All walks must have the same length.");
        }

        var index = new List<UsdtPoint>(count) { new(walks[0][0].Time, startUsdt) };
        var portfolioUsdt = (double)startUsdt;

        for (var day = 1; day < count; day++)
        {
            var memberReturnSum = 0.0;
            foreach (var walk in walks)
            {
                var previous = walk[day - 1].Usdt;
                var current = walk[day].Usdt;
                memberReturnSum += (double)current / previous - 1;
            }

            var portfolioReturn = memberReturnSum / walks.Count;
            portfolioUsdt *= 1 + portfolioReturn;
            index.Add(new UsdtPoint(walks[0][day].Time, (int)Math.Round(portfolioUsdt)));
        }

        return index;
    }

    public static DrawdownProfile Profile(IReadOnlyList<UsdtPoint> series)
    {
        var underwater = WalkDrawdown.FromPeak(series);
        if (underwater.Count == 0)
            return new DrawdownProfile(0, 0, 0, 0);

        var maxDrawdown = underwater.Min(p => p.DrawdownFraction);
        var squared = underwater.Select(p => p.DrawdownFraction * p.DrawdownFraction).ToArray();
        var ulcer = Math.Sqrt(squared.Average());

        var sorted = underwater.Select(p => p.DrawdownFraction).OrderBy(d => d).ToArray();
        var worstCount = Math.Max(1, (int)Math.Ceiling(0.05 * sorted.Length));
        var cdar95 = sorted.Take(worstCount).Average();

        var longestUnderwater = 0.0;
        var currentRun = 0.0;
        foreach (var point in underwater)
        {
            if (point.DrawdownFraction < 0)
            {
                currentRun += 1;
                longestUnderwater = Math.Max(longestUnderwater, currentRun);
            }
            else
                currentRun = 0;
        }

        return new DrawdownProfile(maxDrawdown, ulcer, cdar95, longestUnderwater);
    }
}
