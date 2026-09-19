using FluentAssertions;

namespace Portfolio.Tests;

public sealed class LineHoverTests
{
    [Fact]
    public void NearestByX_PicksCloserSampleNotInterpolatedY()
    {
        var offset = TimeSpan.FromHours(-5);
        var a = new UsdtPoint(new DateTimeOffset(2020, 8, 1, 0, 0, 0, offset), 2000);
        var b = new UsdtPoint(new DateTimeOffset(2020, 8, 2, 0, 0, 0, offset), 5000);
        var series = new[] { a, b };

        LineHover.NearestByX(series, a.Time.AddHours(6)).Should().Be(a);
        LineHover.NearestByX(series, a.Time.AddHours(18)).Should().Be(b);
    }

    [Fact]
    public void Format_ConvertsInstantToUtcMinusFiveAndIntegerUsdt()
    {
        var utc = new DateTimeOffset(2020, 8, 1, 5, 0, 0, TimeSpan.Zero);
        var point = new UsdtPoint(utc, 3456);

        LineHover.Format(point).Should().Be("2020-08-01 00:00 UTC-5 | 3456 USDT");
    }
}
