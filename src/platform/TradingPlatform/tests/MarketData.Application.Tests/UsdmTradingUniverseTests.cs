using FluentAssertions;
using MarketData.Application;
using MarketData.Domain;
using TradingPlatform.Kernel;

namespace MarketData.Application.Tests;

public sealed class UsdmTradingUniverseTests
{
    [Fact]
    public void IsEligible_AcceptsUsdtPerpetualWithCryptoBase()
    {
        var instrument = Build("BTC", "USDT", "Trading");

        UsdmTradingUniverse.IsEligible(instrument).Should().BeTrue();
    }

    [Fact]
    public void IsEligible_RejectsUsdtBase()
    {
        var instrument = Build("USDT", "USDT", "Trading");

        UsdmTradingUniverse.IsEligible(instrument).Should().BeFalse();
    }

    [Fact]
    public void IsEligible_RejectsUsdcBase()
    {
        var instrument = Build("USDC", "USDT", "Trading");

        UsdmTradingUniverse.IsEligible(instrument).Should().BeFalse();
    }

    [Fact]
    public void IsEligible_RejectsNonTradingStatus()
    {
        var instrument = Build("ETH", "USDT", "BREAK");

        UsdmTradingUniverse.IsEligible(instrument).Should().BeFalse();
    }

    [Fact]
    public void FilterEligible_OrdersByExchangeSymbol()
    {
        var instruments = new[]
        {
            Build("DOGE", "USDT", "Trading", "DOGEUSDT"),
            Build("BTC", "USDT", "Trading", "BTCUSDT"),
            Build("USDT", "USDT", "Trading", "USDTUSDT")
        };

        var filtered = UsdmTradingUniverse.FilterEligible(instruments);

        filtered.Select(i => i.ExchangeSymbol).Should().Equal("BTCUSDT", "DOGEUSDT");
    }

    private static Instrument Build(
        string baseAsset,
        string quoteAsset,
        string status,
        string exchangeSymbol = "TESTUSDT") =>
        new()
        {
            Id = new InstrumentId(1),
            Venue = "binance",
            Market = "usdm",
            ContractType = "perpetual",
            ExchangeSymbol = exchangeSymbol,
            BaseAsset = baseAsset,
            QuoteAsset = quoteAsset,
            LastStatus = status
        };
}
