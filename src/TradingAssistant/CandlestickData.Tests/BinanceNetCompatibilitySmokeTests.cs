using Binance.Net.Enums;
using FluentAssertions;
using Xunit;

namespace CandlestickData.Tests;

/// <summary>
/// Smoke tests for Binance.Net and CryptoExchange.Net compatibility.
/// Run after library upgrades to verify basic functionality.
/// </summary>
public class BinanceNetCompatibilitySmokeTests
{
    [Fact]
    public void KlineInterval_EnumValues_AreAccessible()
    {
        KlineInterval.OneMinute.Should().Be(KlineInterval.OneMinute);
        ((int)KlineInterval.OneMinute).Should().Be(60);
        ((int)KlineInterval.OneHour).Should().Be(3600);
    }

    [Fact]
    public void BinanceNet_Assembly_LoadsWithoutError()
    {
        var assembly = typeof(KlineInterval).Assembly;
        assembly.GetName().Name.Should().Be("Binance.Net");
    }
}
