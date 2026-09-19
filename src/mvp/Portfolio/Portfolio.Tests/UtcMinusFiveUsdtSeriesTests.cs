using FluentAssertions;

namespace Portfolio.Tests;

public sealed class UtcMinusFiveUsdtSeriesTests
{
    [Fact]
    public void Generate_InclusiveRange_StartsAndEndsOnUtcMinusFiveMidnight()
    {
        var offset = TimeSpan.FromHours(-5);
        var start = new DateTimeOffset(2020, 8, 1, 0, 0, 0, offset);
        var end = new DateTimeOffset(2026, 9, 19, 0, 0, 0, offset);

        var points = UtcMinusFiveUsdtSeries.Generate();

        points.Should().NotBeEmpty();
        points[0].Time.Should().Be(start);
        points[^1].Time.Should().Be(end);
    }

    [Fact]
    public void Generate_HasTargetPointCount()
    {
        UtcMinusFiveUsdtSeries.Generate().Should().HaveCount(UtcMinusFiveUsdtSeries.TargetPointCount);
    }

    [Fact]
    public void Generate_XValues_AreStrictlyIncreasingByAtLeastOneDay()
    {
        var points = UtcMinusFiveUsdtSeries.Generate();

        for (var i = 1; i < points.Count; i++)
        {
            var delta = points[i].Time - points[i - 1].Time;
            delta.Should().BeGreaterThanOrEqualTo(TimeSpan.FromDays(1));
            (delta.TotalDays % 1).Should().Be(0);
        }
    }

    [Fact]
    public void Generate_YValues_AreIntegersIn2000To5000Inclusive()
    {
        foreach (var point in UtcMinusFiveUsdtSeries.Generate())
        {
            point.Usdt.Should().BeGreaterThanOrEqualTo(2000);
            point.Usdt.Should().BeLessThanOrEqualTo(5000);
        }
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentSeries()
    {
        UtcMinusFiveUsdtSeries.Generate(1).Should().Equal(UtcMinusFiveUsdtSeries.Generate(1));
        UtcMinusFiveUsdtSeries.Generate(1).Should().NotEqual(UtcMinusFiveUsdtSeries.Generate(2));
    }
}
