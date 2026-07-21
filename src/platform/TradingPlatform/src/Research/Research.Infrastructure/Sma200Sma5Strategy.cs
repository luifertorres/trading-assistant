using Research.Application;
using Skender.Stock.Indicators;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

/// <summary>
/// Ivan Scherman SMA200/SMA5 vector logic. At most one open position per vector (no pyramiding).
/// </summary>
public sealed class Sma200Sma5Strategy : ITradingStrategy
{
    private const int RequiredBars = 201;
    private readonly List<OhlcBar> _bars = [];
    private bool _inPosition;

    public void OnBar(BarProcessingContext context)
    {
        _bars.Add(context.Bar);
        if (_bars.Count < RequiredBars)
            return;

        var quotes = _bars
            .Select(b => new Quote
            {
                Date = b.OpenTime.UtcDateTime,
                Open = b.Open,
                High = b.High,
                Low = b.Low,
                Close = b.Close,
                Volume = b.Volume
            })
            .ToList();

        var sma200 = quotes.GetSma(200).ToList();
        var sma5 = quotes.GetSma(5).ToList();
        var lastIndex = _bars.Count - 1;
        var ma200 = sma200[lastIndex].Sma;
        var ma200Previous = sma200[lastIndex - 1].Sma;
        var ma5 = sma5[lastIndex].Sma;
        if (ma200 is null || ma200Previous is null || ma5 is null)
            return;

        var last = _bars[lastIndex];
        var direction = context.Vector.Direction;

        if (_inPosition)
        {
            TryExit(context, last, direction, ma5.Value);
            return;
        }

        if (direction == Direction.Long && IsLongEntry(last, ma200.Value, ma200Previous.Value, ma5.Value))
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.OpenLong, 0, "sma200sma5-enter"), context.Bar);
            _inPosition = true;
            return;
        }

        if (direction == Direction.Short && IsShortEntry(last, ma200.Value, ma200Previous.Value, ma5.Value))
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.OpenShort, 0, "sma200sma5-enter"), context.Bar);
            _inPosition = true;
        }
    }

    private void TryExit(BarProcessingContext context, OhlcBar last, Direction direction, double ma5)
    {
        if (direction == Direction.Long && last.Close > (decimal)ma5)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "sma200sma5-exit"), context.Bar);
            _inPosition = false;
            return;
        }

        if (direction == Direction.Short && last.Close < (decimal)ma5)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "sma200sma5-exit"), context.Bar);
            _inPosition = false;
        }
    }

    private static bool IsShortEntry(OhlcBar last, double ma200, double ma200Previous, double ma5) =>
        ma200Previous > ma200
        && last.Close > last.Open
        && last.Low > (decimal)ma5;

    private static bool IsLongEntry(OhlcBar last, double ma200, double ma200Previous, double ma5) =>
        ma200Previous < ma200
        && last.Close < last.Open
        && last.High < (decimal)ma5;
}
