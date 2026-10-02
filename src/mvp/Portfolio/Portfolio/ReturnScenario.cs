namespace Portfolio;

public enum ReturnScenario
{
    NegativeGrowth,
    PositiveBias
}

public static class ReturnScenarioSeries
{
    public static double Drift(ReturnScenario scenario) => scenario switch
    {
        ReturnScenario.NegativeGrowth => 0,
        ReturnScenario.PositiveBias => UtcMinusFiveUsdtSeries.DailyDrift,
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    public static IReadOnlyList<int> Seeds(ReturnScenario scenario) => scenario switch
    {
        ReturnScenario.NegativeGrowth =>
            [UtcMinusFiveUsdtSeries.DefaultSeed, UtcMinusFiveUsdtSeries.SecondSeed],
        ReturnScenario.PositiveBias =>
        [
            UtcMinusFiveUsdtSeries.DefaultSeed,
            UtcMinusFiveUsdtSeries.SecondSeed,
            UtcMinusFiveUsdtSeries.ThirdSeed
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };
}
