using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public class TimeFrameCodeDay1Tests
{
    [Theory]
    [InlineData("1d")]
    [InlineData("1D")]
    [InlineData("d1")]
    [InlineData("1day")]
    public void Parse_AcceptsDailyAliases(string raw)
    {
        TimeFrameCode.Parse(raw).Should().Be(TimeFrameCode.Day1);
    }

    [Fact]
    public void Day1_Value_Is1D()
    {
        TimeFrameCode.Day1.Value.Should().Be("1D");
    }
}
