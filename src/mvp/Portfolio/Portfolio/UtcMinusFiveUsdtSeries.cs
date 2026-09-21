namespace Portfolio;

public static class UtcMinusFiveUsdtSeries
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-5);
    public static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, Offset);
    public static readonly DateTimeOffset End = new(2026, 9, 19, 0, 0, 0, Offset);

    public const int MinUsdt = 2000;
    public const int MaxUsdt = 5000;
    public const int DefaultSeed = 42;
    public const double DailyChangeRate = 0.05;

    public static int InclusiveDayCount => (int)(End - Start).TotalDays + 1;

    public static IReadOnlyList<UsdtPoint> Generate(int seed = DefaultSeed)
    {
        var random = new Random(seed);
        var points = new List<UsdtPoint>(InclusiveDayCount);
        var usdt = random.Next(MinUsdt, MaxUsdt + 1);

        for (var time = Start; time <= End; time = time.AddDays(1))
        {
            points.Add(new UsdtPoint(time, usdt));

            if (time < End)
            {
                var factor = (random.NextDouble() * 2.0) - 1.0;
                usdt = (int)Math.Round(usdt + usdt * DailyChangeRate * factor);
            }
        }

        return points;
    }
}
