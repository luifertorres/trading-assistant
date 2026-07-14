using Skender.Stock.Indicators;

namespace WebSocketTrading;

public sealed class SmaShortStrategy
{
    public const int RequiredBars = 201;

    public TradeAction Evaluate(IReadOnlyList<Candle> candles, PositionState position)
    {
        if (candles.Count < RequiredBars)
            return TradeAction.Hold;

        var quotes = CandleQuoteMapping.ToQuotes(candles).Validate().ToList();
        var sma200 = quotes.GetSma(200).ToList();
        var sma5 = quotes.GetSma(5).ToList();

        var lastIndex = candles.Count - 1;
        var ma200 = sma200[lastIndex].Sma;
        var ma200Previous = sma200[lastIndex - 1].Sma;
        var ma5 = sma5[lastIndex].Sma;

        if (ma200 is null || ma200Previous is null || ma5 is null)
            return TradeAction.Hold;

        var last = candles[lastIndex];

        if (position == PositionState.Short && last.Close < (decimal)ma5)
            return TradeAction.ExitShort;

        var isMa200Downtrending = ma200Previous > ma200;
        var isBullishCandle = last.Close > last.Open;
        var isAboveMa5 = last.Low > (decimal)ma5;

        if (position == PositionState.Flat && isMa200Downtrending && isBullishCandle && isAboveMa5)
            return TradeAction.EnterShort;

        return TradeAction.Hold;
    }
}
