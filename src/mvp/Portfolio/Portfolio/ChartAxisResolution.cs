namespace Portfolio;

public readonly record struct ChartAxisResolution(TimeSpan XStep, double YStep)
{
    public static ChartAxisResolution DailyUsdt { get; } = new(TimeSpan.FromDays(1), 1);
}
