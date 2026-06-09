using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public class SeriesDescriptorValidationTests
{
    [Fact]
    public void Validate_AllowsPositiveInstrumentIdAndDay1()
    {
        var s = new SeriesDescriptor(new InstrumentId(42), TimeFrameCode.Day1);
        s.Validate();
    }

    [Fact]
    public void Validate_RejectsZeroInstrumentId()
    {
        var s = new SeriesDescriptor(new InstrumentId(0), TimeFrameCode.Day1);
        var act = () => s.Validate();
        act.Should().Throw<ArgumentException>();
    }
}
