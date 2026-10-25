using FluentAssertions;

namespace Portfolio.Tests;

public sealed class DependenceReportTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Build_ThreeWalks_ReturnsPairs42x7_42x13_7x13InOrder()
    {
        var walks = ThreeWalkDictionary();
        var report = DependenceReport.Build(walks);

        report.Pairs.Should().HaveCount(3);
        report.Pairs[0].Left.Should().Be(ChartLegendId.Seed42);
        report.Pairs[0].Right.Should().Be(ChartLegendId.Seed7);
        report.Pairs[1].Left.Should().Be(ChartLegendId.Seed42);
        report.Pairs[1].Right.Should().Be(ChartLegendId.Seed13);
        report.Pairs[2].Left.Should().Be(ChartLegendId.Seed7);
        report.Pairs[2].Right.Should().Be(ChartLegendId.Seed13);
    }

    [Fact]
    public void Build_ComputesPortfolioAndMemberAverageProfiles()
    {
        var walks = ThreeWalkDictionary();
        var report = DependenceReport.Build(walks);

        report.Portfolio.MaxDrawdown.Should().BeLessThanOrEqualTo(0);
        report.MemberAverage.MaxDrawdown.Should().BeLessThanOrEqualTo(0);
        report.Portfolio.UlcerIndex.Should().BeGreaterThanOrEqualTo(0);
        report.MemberAverage.UlcerIndex.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void FormatLines_RendersPercentagesAndNaForNullLift()
    {
        var walks = ThreeWalkDictionary();
        var lines = DependenceReport.Build(walks).FormatLines();

        lines.Should().HaveCount(4);
        lines[0].Should().StartWith("42x7  rho1d ");
        lines[0].Should().Contain("lift-10%");
        lines[3].Should().Contain("portfolio maxDD");
        lines[3].Should().Contain("member avg maxDD");
    }

    private static Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> ThreeWalkDictionary()
    {
        var generated = DependentWalks.Generate(ReturnScenario.PositiveBias);
        return new Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>>
        {
            [ChartLegendId.Seed42] = generated[UtcMinusFiveUsdtSeries.DefaultSeed],
            [ChartLegendId.Seed7] = generated[UtcMinusFiveUsdtSeries.SecondSeed],
            [ChartLegendId.Seed13] = generated[UtcMinusFiveUsdtSeries.ThirdSeed],
        };
    }
}
