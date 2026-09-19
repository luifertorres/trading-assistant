using FluentAssertions;

namespace Portfolio.Tests;

public sealed class TickQuantizerTests
{
    [Fact]
    public void MajorX_ThreeDayRange_EmitsMidnightUtcMinusFiveEachDay()
    {
        var offset = TimeSpan.FromHours(-5);
        var start = new DateTimeOffset(2020, 8, 1, 0, 0, 0, offset);
        var end = new DateTimeOffset(2020, 8, 3, 0, 0, 0, offset);

        var ticks = TickQuantizer.MajorX(start, end, maxTickCount: 10);

        ticks.Should().Equal(start, start.AddDays(1), end);
        ticks.Should().OnlyContain(t => t.TimeOfDay == TimeSpan.Zero && t.Offset == offset);
    }

    [Fact]
    public void MajorX_WhenMaxTickCountIsSmall_StrideIsWholeDays()
    {
        var offset = TimeSpan.FromHours(-5);
        var start = new DateTimeOffset(2020, 8, 1, 0, 0, 0, offset);
        var end = start.AddDays(9);

        var ticks = TickQuantizer.MajorX(start, end, maxTickCount: 3);

        ticks.Should().NotBeEmpty();
        ticks.Count.Should().BeLessThanOrEqualTo(3);
        ticks.Should().OnlyContain(t => t.TimeOfDay == TimeSpan.Zero && t.Offset == offset);
        for (var i = 1; i < ticks.Count; i++)
        {
            var delta = ticks[i] - ticks[i - 1];
            delta.Should().BeGreaterThanOrEqualTo(TimeSpan.FromDays(1));
            (delta.TotalDays % 1).Should().Be(0);
        }
    }

    [Fact]
    public void DailyUsdt_XStepIsOneDay_YStepIsOne()
    {
        ChartAxisResolution.DailyUsdt.XStep.Should().Be(TimeSpan.FromDays(1));
        ChartAxisResolution.DailyUsdt.YStep.Should().Be(1);
    }

    [Fact]
    public void MajorY_SmallRange_EmitsEveryUsdt()
    {
        TickQuantizer.MajorY(2000, 2004, maxTickCount: 10)
            .Should().Equal(2000, 2001, 2002, 2003, 2004);
    }

    [Fact]
    public void MajorY_WhenMaxTickCountIsSmall_StepIsAtLeastOneUsdt()
    {
        var ticks = TickQuantizer.MajorY(2000, 5000, maxTickCount: 5);

        ticks.Should().NotBeEmpty();
        ticks.Count.Should().BeLessThanOrEqualTo(5);
        for (var i = 1; i < ticks.Count; i++)
            (ticks[i] - ticks[i - 1]).Should().BeGreaterThanOrEqualTo(1);
    }
}
