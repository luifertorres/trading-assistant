using FluentAssertions;

namespace Portfolio.Tests;

public sealed class PairDependenceTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void TailCoExceedance_IdenticalSeries_IsOne()
    {
        var series = BuildSeries(100, i => 1000 + i);
        PairDependence.TailCoExceedance(series, series).Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void TailCoExceedance_DisjointWorstDays_IsZero()
    {
        var left = BuildSeries(100, i => 10_000 + i * 10);
        var right = BuildSeries(100, i => 10_000 + i * 10);
        for (var day = 10; day <= 14; day++)
            left[day] = new UsdtPoint(left[day].Time, left[day - 1].Usdt - 500);
        for (var day = 60; day <= 64; day++)
            right[day] = new UsdtPoint(right[day].Time, right[day - 1].Usdt - 500);

        PairDependence.TailCoExceedance(left, right, tailFraction: 0.05).Should().Be(0);
    }

    [Fact]
    public void UnderwaterPearson_IdenticalSeries_IsOne()
    {
        var series = new List<UsdtPoint>
        {
            new(Start, 1000),
            new(Start.AddDays(1), 1100),
            new(Start.AddDays(2), 990),
            new(Start.AddDays(3), 1050),
            new(Start.AddDays(4), 900),
            new(Start.AddDays(5), 950),
        };
        PairDependence.UnderwaterPearson(series, series).Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void CoUnderwaterLift_JointDeepDrawdowns_ComputesRatio()
    {
        var left = BuildSeries(10, i => i < 5 ? 1000 : 800);
        var right = BuildSeries(10, i => i < 5 ? 1000 : 800);
        var lift = PairDependence.CoUnderwaterLift(left, right, depth: 0.10);
        lift.Should().NotBeNull();
        lift!.Value.Should().BeGreaterThan(1.0);
    }

    [Fact]
    public void CoUnderwaterLift_NeverBelowThreshold_ReturnsNull()
    {
        var left = BuildSeries(10, i => 1000 + i);
        var right = BuildSeries(10, i => 1000 + i * 2);
        PairDependence.CoUnderwaterLift(left, right, depth: 0.50).Should().BeNull();
    }

    [Fact]
    public void ReturnDuringWorstEpisodes_AveragesRightReturnOverLeftTopKPeakToTrough()
    {
        var left = new List<UsdtPoint>
        {
            new(Start, 100),
            new(Start.AddDays(1), 110),
            new(Start.AddDays(2), 99),
            new(Start.AddDays(3), 120),
            new(Start.AddDays(4), 90),
            new(Start.AddDays(5), 95),
        };
        var right = new List<UsdtPoint>
        {
            new(Start, 100),
            new(Start.AddDays(1), 100),
            new(Start.AddDays(2), 110),
            new(Start.AddDays(3), 110),
            new(Start.AddDays(4), 120),
            new(Start.AddDays(5), 120),
        };

        var result = PairDependence.ReturnDuringWorstEpisodes(left, right, topK: 1);
        result.Should().BeGreaterThan(0);
    }

    private static List<UsdtPoint> BuildSeries(int count, Func<int, int> usdtAtIndex)
    {
        var points = new List<UsdtPoint>(count);
        for (var i = 0; i < count; i++)
            points.Add(new UsdtPoint(Start.AddDays(i), usdtAtIndex(i)));

        return points;
    }
}
