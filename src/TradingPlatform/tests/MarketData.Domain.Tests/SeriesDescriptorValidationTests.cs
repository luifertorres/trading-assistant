using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public class SeriesDescriptorValidationTests
{
    [Fact]
    public void Validate_AllowsUsdmStyleSymbolAndDay1()
    {
        var s = new SeriesDescriptor("BTCUSDT", TimeFrameCode.Day1);
        s.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmptySymbol(string symbol)
    {
        var s = new SeriesDescriptor(symbol, TimeFrameCode.Day1);
        var act = () => s.Validate();
        act.Should().Throw<ArgumentException>();
    }
}
