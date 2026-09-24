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

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(99)]
    public void Generate_FirstDayY_IsIntegerInInitialRange(int seed)
    {
        var first = UtcMinusFiveUsdtSeries.Generate(seed)[0];

        first.Usdt.Should().BeGreaterThanOrEqualTo(3000);
        first.Usdt.Should().BeLessThanOrEqualTo(5000);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(99)]
    public void Generate_DailyChange_IsWithinOnePercentOfPreviousDay(int seed)
    {
        var points = UtcMinusFiveUsdtSeries.Generate(seed);

        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1].Usdt;
            var current = points[i].Usdt;
            var maxDelta = previous * 0.01;

            ((double)Math.Abs(current - previous)).Should().BeLessThanOrEqualTo(maxDelta + 1.0);
        }
    }

    [Fact]
    public void Sum_AddsUsdtOnMatchingDays()
    {
        var left = UtcMinusFiveUsdtSeries.Generate(1);
        var right = UtcMinusFiveUsdtSeries.Generate(2);
        var summed = UtcMinusFiveUsdtSeries.Sum(left, right);

        summed.Should().HaveCount(left.Count);
        for (var i = 0; i < summed.Count; i++)
        {
            summed[i].Time.Should().Be(left[i].Time);
            summed[i].Usdt.Should().Be(left[i].Usdt + right[i].Usdt);
        }

        for (var i = 1; i < summed.Count; i++)
        {
            var previous = summed[i - 1].Usdt;
            var current = summed[i].Usdt;
            var maxDelta = previous * 0.01;

            ((double)Math.Abs(current - previous)).Should().BeLessThanOrEqualTo(maxDelta + 2.0);
        }
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentSeries()
    {
        UtcMinusFiveUsdtSeries.Generate(1).Should().Equal(UtcMinusFiveUsdtSeries.Generate(1));
        UtcMinusFiveUsdtSeries.Generate(1).Should().NotEqual(UtcMinusFiveUsdtSeries.Generate(2));
    }
}
