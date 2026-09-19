namespace Portfolio;

public static class TickQuantizer
{
    public static IReadOnlyList<DateTimeOffset> MajorX(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        int maxTickCount)
    {
        var ticks = new List<DateTimeOffset>();
        var dayCount = (int)(rangeEnd - rangeStart).TotalDays + 1;
        var strideDays = Math.Max(1, (int)Math.Ceiling(dayCount / (double)Math.Max(1, maxTickCount)));
        var step = TimeSpan.FromDays(ChartAxisResolution.DailyUsdt.XStep.TotalDays * strideDays);

        for (var t = rangeStart; t <= rangeEnd; t = t.Add(step))
            ticks.Add(t);

        return ticks;
    }

    public static IReadOnlyList<int> MajorY(int rangeMin, int rangeMax, int maxTickCount)
    {
        var ticks = new List<int>();
        var span = rangeMax - rangeMin;
        var step = (int)ChartAxisResolution.DailyUsdt.YStep;

        while (span / step + 1 > maxTickCount)
            step = NextNiceStep(step);

        for (var value = rangeMin; value <= rangeMax; value += step)
            ticks.Add(value);

        return ticks;
    }

    private static int NextNiceStep(int step)
    {
        var exp = (int)Math.Floor(Math.Log10(step));
        var pow = (int)Math.Pow(10, exp);
        var mantissa = step / pow;
        return mantissa switch
        {
            1 => 2 * pow,
            2 => 5 * pow,
            _ => 10 * pow
        };
    }
}
