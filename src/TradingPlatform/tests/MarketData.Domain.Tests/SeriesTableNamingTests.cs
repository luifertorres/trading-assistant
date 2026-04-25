using FluentAssertions;
using MarketData.Domain;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public class SeriesTableNamingTests
{
    [Theory]
    [InlineData("BTCUSDT", "BTCUSDT_1D")]
    [InlineData("龙虾USDT", "龙虾USDT_1D")]
    [InlineData("币安人生USDT", "币安人生USDT_1D")]
    [InlineData("我踏马来了USDT", "我踏马来了USDT_1D")]
    [InlineData("1000PEPEUSDT", "1000PEPEUSDT_1D")]
    [InlineData("4USDT", "4USDT_1D")]
    public void ToPhysicalTableName_Day1_AcceptsExchangeSymbols(string symbol, string expected)
    {
        var series = new SeriesDescriptor(symbol, TimeFrameCode.Day1);

        SeriesTableNaming.ToPhysicalTableName(series).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("BTC\"USDT")]
    [InlineData("BTC\u0001USDT")]
    [InlineData("BTC\u007FUSDT")]
    public void ToPhysicalTableName_RejectsUnsafeStorageSymbol(string symbol)
    {
        var series = new SeriesDescriptor(symbol, TimeFrameCode.Day1);

        var act = () => SeriesTableNaming.ToPhysicalTableName(series);

        act.Should().Throw<ArgumentException>();
    }
}
