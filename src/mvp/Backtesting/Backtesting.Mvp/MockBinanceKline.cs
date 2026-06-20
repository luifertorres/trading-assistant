using Binance.Net.Interfaces;

namespace Backtesting.Mvp;

/// <summary>In-memory kline for backtests; implements <see cref="IBinanceKline"/> for parity with live streams.</summary>
public sealed class MockBinanceKline : IBinanceKline
{
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
    public decimal QuoteVolume { get; set; }
    public int TradeCount { get; set; }
    public decimal TakerBuyBaseVolume { get; set; }
    public decimal TakerBuyQuoteVolume { get; set; }

    public static MockBinanceKline Create(
        DateTime openTime,
        DateTime closeTime,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal volume = 1_000m)
    {
        return new MockBinanceKline
        {
            OpenTime = openTime,
            CloseTime = closeTime,
            OpenPrice = open,
            HighPrice = high,
            LowPrice = low,
            ClosePrice = close,
            Volume = volume,
            QuoteVolume = volume * close,
            TradeCount = 100,
            TakerBuyBaseVolume = volume * 0.5m,
            TakerBuyQuoteVolume = volume * close * 0.5m
        };
    }
}
