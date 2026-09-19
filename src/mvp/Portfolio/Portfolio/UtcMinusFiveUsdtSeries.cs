namespace Portfolio;

public static class UtcMinusFiveUsdtSeries
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-5);
    public static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, Offset);
    public static readonly DateTimeOffset End = new(2026, 9, 19, 0, 0, 0, Offset);

    public const int MinUsdt = 2000;
    public const int MaxUsdt = 5000;
    public const int DefaultSeed = 42;
    public const int TargetPointCount = 100;

    public static IReadOnlyList<UsdtPoint> Generate(int seed = DefaultSeed)
    {
        var random = new Random(seed);
        var all = new List<UsdtPoint>();

        for (var time = Start; time <= End; time = time.AddDays(1))
            all.Add(new UsdtPoint(time, random.Next(MinUsdt, MaxUsdt + 1)));

        return Subsample(all, TargetPointCount, random);
    }

    private static IReadOnlyList<UsdtPoint> Subsample(IReadOnlyList<UsdtPoint> all, int targetCount, Random random)
    {
        if (all.Count <= targetCount)
            return all.ToList();

        var interiorIndices = Enumerable
            .Range(1, all.Count - 2)
            .OrderBy(_ => random.Next())
            .Take(targetCount - 2)
            .OrderBy(i => i);

        var selected = new List<UsdtPoint>(targetCount) { all[0] };
        foreach (var index in interiorIndices)
            selected.Add(all[index]);
        selected.Add(all[^1]);

        return selected;
    }
}
