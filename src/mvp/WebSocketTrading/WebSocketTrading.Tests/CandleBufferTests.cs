using FluentAssertions;

namespace WebSocketTrading.Tests;

public sealed class CandleBufferTests
{
    [Fact]
    public void Add_WhenOverCapacity_DropsOldestCandle()
    {
        var buffer = new CandleBuffer(capacity: 3);
        var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        buffer.Add(Build(t0, 1m));
        buffer.Add(Build(t0.AddMinutes(5), 2m));
        buffer.Add(Build(t0.AddMinutes(10), 3m));
        buffer.Add(Build(t0.AddMinutes(15), 4m));

        buffer.Candles.Should().HaveCount(3);
        buffer.Candles[0].Close.Should().Be(2m);
        buffer.Candles[^1].Close.Should().Be(4m);
    }

    private static Candle Build(DateTime date, decimal close) =>
        new()
        {
            Date = date,
            Open = close,
            High = close + 1m,
            Low = close - 1m,
            Close = close
        };
}
