using FluentAssertions;

namespace Portfolio.Tests;

public sealed class DependentWalksTests
{
    [Fact]
    public void Generate_PositiveBias_MatchesUtcMinusFiveUsdtSeriesGenerate()
    {
        var expected = DependentWalks.Generate(ReturnScenario.PositiveBias);
        foreach (var seed in ReturnScenarioSeries.Seeds(ReturnScenario.PositiveBias))
            expected[seed].Should().Equal(UtcMinusFiveUsdtSeries.Generate(seed, UtcMinusFiveUsdtSeries.DailyDrift));
    }

    [Theory]
    [InlineData(ReturnScenario.PositiveBias)]
    [InlineData(ReturnScenario.SharedFactor)]
    [InlineData(ReturnScenario.CrashCoupled)]
    [InlineData(ReturnScenario.ComplementaryRegimes)]
    public void Generate_ThreeSeedScenarios_HaveExpectedShape(ReturnScenario scenario)
    {
        var walks = DependentWalks.Generate(scenario);
        walks.Should().HaveCount(3);
        walks.Keys.Should().Equal(
            UtcMinusFiveUsdtSeries.DefaultSeed,
            UtcMinusFiveUsdtSeries.SecondSeed,
            UtcMinusFiveUsdtSeries.ThirdSeed);

        var anchor = walks[UtcMinusFiveUsdtSeries.DefaultSeed];
        anchor.Should().HaveCount(UtcMinusFiveUsdtSeries.InclusiveDayCount);
        foreach (var series in walks.Values)
        {
            series.Should().HaveCount(UtcMinusFiveUsdtSeries.InclusiveDayCount);
            series.Should().OnlyContain(p => p.Usdt > 0);
            for (var i = 0; i < series.Count; i++)
                series[i].Time.Should().Be(anchor[i].Time);
        }

        var secondRun = DependentWalks.Generate(scenario);
        foreach (var seed in walks.Keys)
            walks[seed].Should().Equal(secondRun[seed]);
    }

    [Fact]
    public void PositiveBias_PairwiseDailyCorrelationAndTailAreNearIndependent()
    {
        var walks = WalksByLegend(ReturnScenario.PositiveBias);
        AssertPairBounds(walks, maxAbsRho1d: 0.1, maxTail: 0.12);
    }

    [Fact]
    public void SharedFactor_PairwiseDailyCorrelationIsHigh()
    {
        var walks = WalksByLegend(ReturnScenario.SharedFactor);
        foreach (var (left, right) in Pairs(walks))
        {
            SeriesCorrelation.LogReturnPearson(left, right)!.Value.Should().BeGreaterThan(0.4);
        }
    }

    [Fact]
    public void CrashCoupled_LowDailyCorrelationButHighTailOverlap()
    {
        var walks = WalksByLegend(ReturnScenario.CrashCoupled);
        foreach (var (left, right) in Pairs(walks))
        {
            SeriesCorrelation.LogReturnPearson(left, right)!.Value.Should().BeLessThan(0.3);
            PairDependence.TailCoExceedance(left, right).Should().BeGreaterThan(0.15);
        }
    }

    [Fact]
    public void ComplementaryRegimes_Seed42And7_ShowOppositeHorizonsAndEpisodeOffset()
    {
        var walks = WalksByLegend(ReturnScenario.ComplementaryRegimes);
        var left = walks[ChartLegendId.Seed42];
        var right = walks[ChartLegendId.Seed7];

        Math.Abs(SeriesCorrelation.LogReturnPearson(left, right)!.Value).Should().BeLessThan(0.15);
        SeriesCorrelation.LogReturnPearson(left, right, horizonDays: 30)!.Value.Should().BeLessThan(-0.2);
        PairDependence.ReturnDuringWorstEpisodes(left, right).Should().BeGreaterThan(0);
    }

    private static Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> WalksByLegend(ReturnScenario scenario)
    {
        var generated = DependentWalks.Generate(scenario);
        return new Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>>
        {
            [ChartLegendId.Seed42] = generated[UtcMinusFiveUsdtSeries.DefaultSeed],
            [ChartLegendId.Seed7] = generated[UtcMinusFiveUsdtSeries.SecondSeed],
            [ChartLegendId.Seed13] = generated[UtcMinusFiveUsdtSeries.ThirdSeed],
        };
    }

    private static void AssertPairBounds(
        Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> walks,
        double maxAbsRho1d,
        double maxTail)
    {
        foreach (var (left, right) in Pairs(walks))
        {
            Math.Abs(SeriesCorrelation.LogReturnPearson(left, right)!.Value).Should().BeLessThan(maxAbsRho1d);
            PairDependence.TailCoExceedance(left, right).Should().BeLessThan(maxTail);
        }
    }

    private static IEnumerable<(IReadOnlyList<UsdtPoint> Left, IReadOnlyList<UsdtPoint> Right)> Pairs(
        Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> walks)
    {
        var ids = walks.Keys.OrderBy(id => id).ToArray();
        for (var i = 0; i < ids.Length; i++)
        {
            for (var j = i + 1; j < ids.Length; j++)
                yield return (walks[ids[i]], walks[ids[j]]);
        }
    }
}
