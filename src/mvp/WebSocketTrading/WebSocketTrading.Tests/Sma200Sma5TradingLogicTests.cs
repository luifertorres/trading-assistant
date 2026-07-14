using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class Sma200Sma5TradingLogicTests
{
    [Fact]
    public void EvaluateShort_WhenWarmupIncomplete_ReturnsHold()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Short);
        var candles = BuildFlatCandles(200, 100m);

        var action = logic.Evaluate(candles, PositionState.OutOfMarket);

        action.Should().Be(TradeAction.Hold);
    }

    [Fact]
    public void EvaluateShort_WhenEntryConditionsMet_ReturnsEnterShort()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Short);
        var candles = BuildShortEntryCandles();

        var action = logic.Evaluate(candles, PositionState.OutOfMarket);

        action.Should().Be(TradeAction.EnterShort);
    }

    [Fact]
    public void EvaluateShort_WhenAlreadyShortAndEntryConditionsMet_ReturnsEnterShort()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Short);
        var candles = BuildShortEntryCandles();

        var action = logic.Evaluate(candles, PositionState.Short);

        action.Should().Be(TradeAction.EnterShort);
    }

    [Fact]
    public void EvaluateShort_WhenShortAndCloseBelowMa5_ReturnsExitShort()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Short);
        var candles = BuildFlatCandles(200, 100m);
        candles.Add(new Candle
        {
            Date = candles[^1].Date.AddMinutes(5),
            Open = 105m,
            High = 106m,
            Low = 97m,
            Close = 98m
        });

        var action = logic.Evaluate(candles, PositionState.Short);

        action.Should().Be(TradeAction.ExitShort);
    }

    [Fact]
    public void EvaluateShort_WhenOutOfMarketAndConditionsUnmet_ReturnsHold()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Short);
        var candles = BuildFlatCandles(200, 100m);
        candles.Add(new Candle
        {
            Date = candles[^1].Date.AddMinutes(5),
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100m
        });

        var action = logic.Evaluate(candles, PositionState.OutOfMarket);

        action.Should().Be(TradeAction.Hold);
    }

    [Fact]
    public void EvaluateLong_WhenEntryConditionsMet_ReturnsEnterLong()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Long);
        var candles = BuildLongEntryCandles();

        var action = logic.Evaluate(candles, PositionState.OutOfMarket);

        action.Should().Be(TradeAction.EnterLong);
    }

    [Fact]
    public void EvaluateLong_WhenAlreadyLongAndEntryConditionsMet_ReturnsEnterLong()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Long);
        var candles = BuildLongEntryCandles();

        var action = logic.Evaluate(candles, PositionState.Long);

        action.Should().Be(TradeAction.EnterLong);
    }

    [Fact]
    public void EvaluateLong_WhenLongAndCloseAboveMa5_ReturnsExitLong()
    {
        var logic = new Sma200Sma5TradingLogic(Direction.Long);
        var candles = BuildFlatCandles(200, 100m);
        candles.Add(new Candle
        {
            Date = candles[^1].Date.AddMinutes(5),
            Open = 95m,
            High = 103m,
            Low = 94m,
            Close = 102m
        });

        var action = logic.Evaluate(candles, PositionState.Long);

        action.Should().Be(TradeAction.ExitLong);
    }

    private static List<Candle> BuildShortEntryCandles()
    {
        var candles = BuildFlatCandles(195, 110m);
        for (var i = 0; i < 5; i++)
        {
            candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 100m, 101m, 99m, 100m));
        }

        candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 97m, 103m, 101.5m, 102m));
        return candles;
    }

    private static List<Candle> BuildLongEntryCandles()
    {
        var candles = BuildFlatCandles(195, 90m);
        for (var i = 0; i < 5; i++)
        {
            candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 110m, 111m, 109m, 110m));
        }

        candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 103m, 98.5m, 97m, 98m));
        return candles;
    }

    private static Candle BuildCandle(DateTime date, decimal open, decimal high, decimal low, decimal close) =>
        new()
        {
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close
        };

    private static List<Candle> BuildFlatCandles(int count, decimal close)
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<Candle>(count);

        for (var i = 0; i < count; i++)
        {
            candles.Add(new Candle
            {
                Date = start.AddMinutes(i * 5),
                Open = close,
                High = close + 1m,
                Low = close - 1m,
                Close = close
            });
        }

        return candles;
    }
}
