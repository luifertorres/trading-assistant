using Skender.Stock.Indicators;

namespace WebSocketTrading;

internal static class CandleQuoteMapping
{
    internal static Quote ToQuote(Candle candle) =>
        new()
        {
            Date = candle.Date,
            Open = candle.Open,
            High = candle.High,
            Low = candle.Low,
            Close = candle.Close,
            Volume = candle.Volume
        };

    internal static IEnumerable<Quote> ToQuotes(IReadOnlyList<Candle> candles) =>
        candles.Select(CandleQuoteMapping.ToQuote);
}
