using FluentAssertions;

namespace Portfolio.Tests;

public sealed class DrawdownEpisodesTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Find_HandSeries_ReturnsRecoveredAndOpenEpisodes()
    {
        var series = new List<UsdtPoint>
        {
            new(Start, 100),
            new(Start.AddDays(1), 110),
            new(Start.AddDays(2), 99),
            new(Start.AddDays(3), 105),
            new(Start.AddDays(4), 120),
            new(Start.AddDays(5), 90),
            new(Start.AddDays(6), 95),
        };

        var episodes = DrawdownEpisodes.Find(series);

        episodes.Should().HaveCount(2);
        episodes[0].PeakIndex.Should().Be(1);
        episodes[0].TroughIndex.Should().Be(2);
        episodes[0].RecoveryIndex.Should().Be(4);
        episodes[0].Depth.Should().BeApproximately(99.0 / 110 - 1, 1e-9);

        episodes[1].PeakIndex.Should().Be(4);
        episodes[1].TroughIndex.Should().Be(5);
        episodes[1].RecoveryIndex.Should().BeNull();
        episodes[1].Depth.Should().BeApproximately(90.0 / 120 - 1, 1e-9);
    }

    [Fact]
    public void Find_MonotonicRise_ReturnsEmpty()
    {
        var series = BuildSeries(5, i => 100 + i * 10);
        DrawdownEpisodes.Find(series).Should().BeEmpty();
    }

    private static List<UsdtPoint> BuildSeries(int count, Func<int, int> usdtAtIndex)
    {
        var points = new List<UsdtPoint>(count);
        for (var i = 0; i < count; i++)
            points.Add(new UsdtPoint(Start.AddDays(i), usdtAtIndex(i)));

        return points;
    }
}
