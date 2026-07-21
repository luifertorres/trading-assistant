using FluentAssertions;
using TradingPlatform.Kernel;

namespace MarketData.Domain.Tests;

public sealed class AssetTests
{
    [Fact]
    public void FromUsdmExchangeSymbol_BuildsCanonicalAsset()
    {
        var asset = Asset.FromUsdmExchangeSymbol("BTCUSDT");

        asset.Value.Should().Be("binance:usdm:BTCUSDT");
    }

    [Fact]
    public void Parse_AcceptsBrokerVenueSymbol()
    {
        var asset = Asset.Parse("binance:usdm:ETHUSDT");

        asset.Value.Should().Be("binance:usdm:ETHUSDT");
    }

    [Fact]
    public void TryParse_RejectsInvalidFormat()
    {
        Asset.TryParse("BTCUSDT", out _).Should().BeFalse();
        Asset.TryParse("binance:usdm", out _).Should().BeFalse();
        Asset.TryParse("", out _).Should().BeFalse();
    }

    [Fact]
    public void Parse_ThrowsOnInvalidFormat()
    {
        var act = () => Asset.Parse("BTCUSDT");

        act.Should().Throw<FormatException>();
    }
}
