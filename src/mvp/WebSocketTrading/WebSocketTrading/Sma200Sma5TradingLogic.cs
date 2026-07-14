using Skender.Stock.Indicators;

namespace WebSocketTrading;

public sealed class Sma200Sma5TradingLogic(Direction direction)
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

        return direction switch
        {
            Direction.Short => EvaluateShort(last, position, ma200.Value, ma200Previous.Value, ma5.Value),
            Direction.Long => EvaluateLong(last, position, ma200.Value, ma200Previous.Value, ma5.Value),
            _ => TradeAction.Hold
        };
    }

    private static TradeAction EvaluateShort(
        Candle last,
        PositionState position,
        double ma200,
        double ma200Previous,
        double ma5)
    {
        if (position == PositionState.Short && last.Close < (decimal)ma5)
            return TradeAction.ExitShort;

        var isMa200Downtrending = ma200Previous > ma200;
        var isBullishCandle = last.Close > last.Open;
        var isAboveMa5 = last.Low > (decimal)ma5;

        if (isMa200Downtrending && isBullishCandle && isAboveMa5)
            return TradeAction.EnterShort;

        return TradeAction.Hold;
    }

    private static TradeAction EvaluateLong(
        Candle last,
        PositionState position,
        double ma200,
        double ma200Previous,
        double ma5)
    {
        if (position == PositionState.Long && last.Close > (decimal)ma5)
            return TradeAction.ExitLong;

        var isMa200Uptrending = ma200Previous < ma200;
        var isBearishCandle = last.Close < last.Open;
        var isBelowMa5 = last.High < (decimal)ma5;

        if (isMa200Uptrending && isBearishCandle && isBelowMa5)
            return TradeAction.EnterLong;

        return TradeAction.Hold;
    }
}
