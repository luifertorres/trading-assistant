using CandlestickData.Domain;
using FluentAssertions;
using Xunit;

namespace CandlestickData.Tests;

public class TimeFrameExtensionsTests
{
    [Theory]
    [InlineData("1m", TimeFrame.OneMinute)]
    [InlineData("5m", TimeFrame.FiveMinutes)]
    [InlineData("15m", TimeFrame.FifteenMinutes)]
    [InlineData("1h", TimeFrame.OneHour)]
    [InlineData("1d", TimeFrame.OneDay)]
    [InlineData("1M", TimeFrame.OneMonth)]
    public void TryParseFromShortString_WithValidInput_ReturnsTrue(string input, TimeFrame expected)
    {
        var result = TimeFrameExtensions.TryParseFromShortString(input, out var timeFrame);

        result.Should().BeTrue();
        timeFrame.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("99m")]
    public void TryParseFromShortString_WithInvalidInput_ReturnsFalse(string input)
    {
        var result = TimeFrameExtensions.TryParseFromShortString(input, out var timeFrame);

        result.Should().BeFalse();
        timeFrame.Should().Be(default(TimeFrame));
    }

    [Fact]
    public void ToShortString_RoundTripsWithTryParse()
    {
        foreach (TimeFrame tf in Enum.GetValues<TimeFrame>())
        {
            var shortStr = tf.ToShortString();
            TimeFrameExtensions.TryParseFromShortString(shortStr, out var parsed).Should().BeTrue();
            parsed.Should().Be(tf);
        }
    }
}
