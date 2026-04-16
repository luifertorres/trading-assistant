using FluentAssertions;
using MarketData.Domain;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public class SeriesTableNamingTests
{
    [Fact]
    public void ToPhysicalTableName_Day1_Uses1DChartCode()
    {
        var series = new SeriesDescriptor("BTCUSDT", TimeFrameCode.Day1);
        SeriesTableNaming.ToPhysicalTableName(series).Should().Be("BTCUSDT_1D");
    }

    [Fact]
    public void ToPhysicalTableName_RejectsInvalidSymbol()
    {
        var series = new SeriesDescriptor("BTC-USDT", TimeFrameCode.Day1);
        var act = () => SeriesTableNaming.ToPhysicalTableName(series);
        act.Should().Throw<ArgumentException>();
    }
}
