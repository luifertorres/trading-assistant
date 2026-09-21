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
    public void Generate_HasOnePointPerDayInInclusiveRange()
    {
        UtcMinusFiveUsdtSeries.Generate().Should().HaveCount(UtcMinusFiveUsdtSeries.InclusiveDayCount);
    }

    [Fact]
    public void Generate_XValues_IncreaseByExactlyOneDay()
    {
        var points = UtcMinusFiveUsdtSeries.Generate();

        for (var i = 1; i < points.Count; i++)
            (points[i].Time - points[i - 1].Time).Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public void Generate_FirstDayY_IsIntegerInInitialRange()
    {
        var first = UtcMinusFiveUsdtSeries.Generate()[0];

        first.Usdt.Should().BeGreaterThanOrEqualTo(UtcMinusFiveUsdtSeries.MinUsdt);
        first.Usdt.Should().BeLessThanOrEqualTo(UtcMinusFiveUsdtSeries.MaxUsdt);
    }

    [Fact]
    public void Generate_DailyChange_IsWithinFivePercentOfPreviousDay()
    {
        var points = UtcMinusFiveUsdtSeries.Generate();

        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1].Usdt;
            var current = points[i].Usdt;
            var maxDelta = previous * 0.05;

            ((double)Math.Abs(current - previous)).Should().BeLessThanOrEqualTo(maxDelta + 1.0);
        }
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentSeries()
    {
        UtcMinusFiveUsdtSeries.Generate(1).Should().Equal(UtcMinusFiveUsdtSeries.Generate(1));
        UtcMinusFiveUsdtSeries.Generate(1).Should().NotEqual(UtcMinusFiveUsdtSeries.Generate(2));
    }
}
