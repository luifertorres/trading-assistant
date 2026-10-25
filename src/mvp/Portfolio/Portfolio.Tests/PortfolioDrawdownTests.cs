using FluentAssertions;

namespace Portfolio.Tests;

public sealed class PortfolioDrawdownTests
{
    private static readonly DateTimeOffset Start = new(2020, 8, 1, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void EqualWeightIndex_OppositeDailyMoves_StaysFlat()
    {
        var left = new List<UsdtPoint>
        {
            new(Start, 10_000),
            new(Start.AddDays(1), 11_000),
            new(Start.AddDays(2), 12_100),
        };
        var right = new List<UsdtPoint>
        {
            new(Start, 10_000),
            new(Start.AddDays(1), 9_000),
            new(Start.AddDays(2), 8_100),
        };

        var index = PortfolioDrawdown.EqualWeightIndex([left, right]);
        index[^1].Usdt.Should().Be(10_000);
    }

    [Fact]
    public void Profile_HandSeries_ReturnsMaxDrawdownUlcerCdar95AndLongestUnderwater()
    {
        var series = new List<UsdtPoint>
        {
            new(Start, 100),
            new(Start.AddDays(1), 110),
            new(Start.AddDays(2), 88),
            new(Start.AddDays(3), 99),
            new(Start.AddDays(4), 110),
        };

        var profile = PortfolioDrawdown.Profile(series);

        profile.MaxDrawdown.Should().BeApproximately(88.0 / 110 - 1, 1e-9);
        profile.UlcerIndex.Should().BeGreaterThan(0);
        profile.Cdar95.Should().BeLessThanOrEqualTo(profile.MaxDrawdown);
        profile.LongestUnderwaterDays.Should().BeGreaterThan(0);
    }
}
