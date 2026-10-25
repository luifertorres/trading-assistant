namespace Portfolio;

public enum ReturnScenario
{
    NegativeGrowth,
    PositiveBias,
    SharedFactor,
    CrashCoupled,
    ComplementaryRegimes
}

public static class ReturnScenarioSeries
{
    public static double Drift(ReturnScenario scenario) => scenario switch
    {
        ReturnScenario.NegativeGrowth => 0,
        ReturnScenario.PositiveBias => UtcMinusFiveUsdtSeries.DailyDrift,
        ReturnScenario.SharedFactor => UtcMinusFiveUsdtSeries.DailyDrift,
        ReturnScenario.CrashCoupled => UtcMinusFiveUsdtSeries.DailyDrift,
        ReturnScenario.ComplementaryRegimes => UtcMinusFiveUsdtSeries.DailyDrift,
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    public static IReadOnlyList<int> Seeds(ReturnScenario scenario) => scenario switch
    {
        ReturnScenario.NegativeGrowth =>
            [UtcMinusFiveUsdtSeries.DefaultSeed, UtcMinusFiveUsdtSeries.SecondSeed],
        ReturnScenario.PositiveBias or ReturnScenario.SharedFactor or ReturnScenario.CrashCoupled
            or ReturnScenario.ComplementaryRegimes =>
        [
            UtcMinusFiveUsdtSeries.DefaultSeed,
            UtcMinusFiveUsdtSeries.SecondSeed,
            UtcMinusFiveUsdtSeries.ThirdSeed
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };
}
