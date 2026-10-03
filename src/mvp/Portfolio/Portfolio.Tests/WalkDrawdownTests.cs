using FluentAssertions;

namespace Portfolio.Tests;

public sealed class WalkDrawdownTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void FromPeak_AfterDrop_IsFractionBelowRunningPeak()
    {
        var series = new List<UsdtPoint>
        {
            new(Start, 100),
            new(Start.AddDays(1), 110),
            new(Start.AddDays(2), 99),
            new(Start.AddDays(3), 105),
        };

        var result = WalkDrawdown.FromPeak(series);

        result.Should().HaveCount(4);
        result[0].DrawdownFraction.Should().Be(0);
        result[1].DrawdownFraction.Should().Be(0);
        result[2].DrawdownFraction.Should().BeApproximately(99.0 / 110 - 1, 1e-9);
        result[3].DrawdownFraction.Should().BeApproximately(105.0 / 110 - 1, 1e-9);
    }

    [Fact]
    public void FromPeak_EmptySeries_ReturnsEmpty()
    {
        WalkDrawdown.FromPeak([]).Should().BeEmpty();
    }

    [Fact]
    public void FromPeak_NonPositiveUsdt_Throws()
    {
        var series = new List<UsdtPoint> { new(Start, 0) };

        var act = () => WalkDrawdown.FromPeak(series);

        act.Should().Throw<ArgumentException>();
    }
}
