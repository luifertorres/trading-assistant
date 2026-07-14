using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class SmaShortStrategyTests
{
    private readonly SmaShortStrategy _strategy = new();

    [Fact]
    public void Evaluate_WhenWarmupIncomplete_ReturnsHold()
    {
        var candles = BuildFlatCandles(200, 100m);

        var action = _strategy.Evaluate(candles, PositionState.Flat);

        action.Should().Be(TradeAction.Hold);
    }

    [Fact]
    public void Evaluate_WhenShortEntryConditionsMet_ReturnsEnterShort()
    {
        var candles = BuildFlatCandles(195, 110m);
        for (var i = 0; i < 5; i++)
        {
            candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 100m, 101m, 99m, 100m));
        }

        candles.Add(BuildCandle(candles[^1].Date.AddMinutes(5), 97m, 103m, 101.5m, 102m));

        var action = _strategy.Evaluate(candles, PositionState.Flat);

        action.Should().Be(TradeAction.EnterShort);
    }

    [Fact]
    public void Evaluate_WhenShortAndCloseBelowMa5_ReturnsExitShort()
    {
        var candles = BuildFlatCandles(200, 100m);
        candles.Add(new Candle
        {
            Date = candles[^1].Date.AddMinutes(5),
            Open = 105m,
            High = 106m,
            Low = 97m,
            Close = 98m
        });

        var action = _strategy.Evaluate(candles, PositionState.Short);

        action.Should().Be(TradeAction.ExitShort);
    }

    [Fact]
    public void Evaluate_WhenFlatAndConditionsUnmet_ReturnsHold()
    {
        var candles = BuildFlatCandles(200, 100m);
        candles.Add(new Candle
        {
            Date = candles[^1].Date.AddMinutes(5),
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100m
        });

        var action = _strategy.Evaluate(candles, PositionState.Flat);

        action.Should().Be(TradeAction.Hold);
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
