using FluentAssertions;

namespace Portfolio.Tests;

public sealed class ReturnScenarioTests
{
    [Fact]
    public void NegativeGrowth_UsesZeroDriftAndSeeds42And7()
    {
        ReturnScenarioSeries.Drift(ReturnScenario.NegativeGrowth).Should().Be(0);
        ReturnScenarioSeries.Seeds(ReturnScenario.NegativeGrowth)
            .Should().Equal(UtcMinusFiveUsdtSeries.DefaultSeed, UtcMinusFiveUsdtSeries.SecondSeed);
    }

    [Fact]
    public void PositiveBias_UsesDailyDriftAndSeeds42_7_13()
    {
        ReturnScenarioSeries.Drift(ReturnScenario.PositiveBias).Should().Be(UtcMinusFiveUsdtSeries.DailyDrift);
        ReturnScenarioSeries.Seeds(ReturnScenario.PositiveBias)
            .Should().Equal(
                UtcMinusFiveUsdtSeries.DefaultSeed,
                UtcMinusFiveUsdtSeries.SecondSeed,
                UtcMinusFiveUsdtSeries.ThirdSeed);
    }
}
