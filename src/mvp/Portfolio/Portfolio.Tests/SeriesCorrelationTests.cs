using FluentAssertions;

namespace Portfolio.Tests;

public sealed class SeriesCorrelationTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void RollingLogReturnPearson_IdenticalSeries_ReturnsOneOnLastDate()
    {
        var series = BuildSeries(31, i => 1000 + i);
        var result = SeriesCorrelation.RollingLogReturnPearson(series, series, window: 30);

        result.Should().HaveCount(1);
        result[0].Time.Should().Be(Start.AddDays(30));
        result[0].Correlation.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void RollingLogReturnPearson_OppositeLogReturns_ReturnsNegativeOne()
    {
        var (left, right) = BuildMirroredLogReturnSeries(31, baseUsdt: 1_000_000_000);
        var result = SeriesCorrelation.RollingLogReturnPearson(left, right, window: 30);

        result.Should().HaveCount(1);
        result[0].Correlation.Should().BeApproximately(-1.0, 1e-9);
    }

    [Fact]
    public void RollingLogReturnPearson_FewerThanWindowReturns_ReturnsEmpty()
    {
        var series = BuildSeries(30, i => 1000 + i);
        var result = SeriesCorrelation.RollingLogReturnPearson(series, series, window: 30);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RollingLogReturnPearson_ZeroVarianceWindow_IsOmitted()
    {
        var left = BuildSeries(31, _ => 1000);
        var right = BuildSeries(31, i => 1000 + i);
        var result = SeriesCorrelation.RollingLogReturnPearson(left, right, window: 30);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RollingLogReturnPearson_MismatchedLength_Throws()
    {
        var left = BuildSeries(31, i => 1000 + i);
        var right = BuildSeries(30, i => 1000 + i);

        var act = () => SeriesCorrelation.RollingLogReturnPearson(left, right);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RollingLogReturnPearson_MismatchedTimestamps_Throws()
    {
        var left = BuildSeries(31, i => 1000 + i);
        var right = BuildSeries(31, i => 1000 + i);
        right[0] = new UsdtPoint(Start.AddDays(1), right[0].Usdt);

        var act = () => SeriesCorrelation.RollingLogReturnPearson(left, right);

        act.Should().Throw<ArgumentException>();
    }

    private static List<UsdtPoint> BuildSeries(int count, Func<int, int> usdtAtIndex)
    {
        var points = new List<UsdtPoint>(count);
        for (var i = 0; i < count; i++)
            points.Add(new UsdtPoint(Start.AddDays(i), usdtAtIndex(i)));

        return points;
    }

    private static (List<UsdtPoint> Left, List<UsdtPoint> Right) BuildMirroredLogReturnSeries(
        int count,
        int baseUsdt)
    {
        var left = new List<UsdtPoint>(count) { new(Start, baseUsdt) };
        var right = new List<UsdtPoint>(count) { new(Start, baseUsdt) };
        var leftUsdt = (double)baseUsdt;
        var rightUsdt = (double)baseUsdt;

        for (var i = 1; i < count; i++)
        {
            var dailyLogReturn = 0.001 * i;
            leftUsdt *= Math.Exp(dailyLogReturn);
            rightUsdt *= Math.Exp(-dailyLogReturn);
            left.Add(new UsdtPoint(Start.AddDays(i), (int)Math.Round(leftUsdt)));
            right.Add(new UsdtPoint(Start.AddDays(i), (int)Math.Round(rightUsdt)));
        }

        return (left, right);
    }
}
