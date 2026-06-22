using System.Globalization;
using Research.Application;
using Skender.Stock.Indicators;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

/// <summary>
/// RSI(5) cross up through 10; SL = min low of last 6 bars (24h on 4H); TP = entry * (1 + takeProfitPct).
/// Parameters: takeProfitPct (default 0.08), rsiExit (default 70), lookbackBars (default 6).
/// </summary>
public sealed class Rsi5ExtremeStrategy : ITradingStrategy
{
    private readonly List<OhlcBar> _bars = [];
    private bool _inPosition;
    private decimal _stopLoss;
    private decimal _takeProfit;

    public void OnBar(BarProcessingContext context)
    {
        _bars.Add(context.Bar);
        var takeProfitPct = ParseDecimal(context.Vector.Parameters, "takeProfitPct", 0.08m);
        var rsiExit = ParseDecimal(context.Vector.Parameters, "rsiExit", 70m);
        var lookback = int.Parse(context.Vector.Parameters.GetValueOrDefault("lookbackBars", "6"), CultureInfo.InvariantCulture);

        if (_inPosition)
        {
            TryExit(context, rsiExit);
            return;
        }

        if (_bars.Count < lookback + 2)
            return;

        var rsiNow = GetRsi(_bars.Count - 1);
        var rsiPrev = GetRsi(_bars.Count - 2);
        if (rsiPrev < 10m && rsiNow > 10m)
        {
            var window = _bars.TakeLast(lookback).ToList();
            _stopLoss = window.Min(b => b.Low);
            var entry = context.Bar.Close;
            _takeProfit = entry * (1 + takeProfitPct);
            context.Sink.OnIntent(
                new OrderIntent(
                    OrderIntentKind.OpenLong,
                    0,
                    "rsi5-cross",
                    StopLossPrice: _stopLoss,
                    TakeProfitPrice: _takeProfit),
                context.Bar);
            _inPosition = true;
        }
    }

    private void TryExit(BarProcessingContext context, decimal rsiExit)
    {
        var bar = context.Bar;
        if (bar.Low <= _stopLoss)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "sl", ExitPrice: _stopLoss), bar);
            _inPosition = false;
            return;
        }

        if (bar.High >= _takeProfit)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "tp", ExitPrice: _takeProfit), bar);
            _inPosition = false;
            return;
        }

        var rsi = GetRsi(_bars.Count - 1);
        if (rsi >= rsiExit)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "rsi-exit"), bar);
            _inPosition = false;
        }
    }

    private decimal GetRsi(int barIndex)
    {
        const int period = 5;
        if (barIndex < period)
            return 0;

        var quotes = _bars
            .Take(barIndex + 1)
            .Select(b => new Quote
            {
                Date = b.OpenTime.UtcDateTime,
                Close = b.Close
            });
        var point = quotes.GetRsi(period).LastOrDefault();
        return point?.Rsi is double r ? (decimal)r : 0m;
    }

    private static decimal ParseDecimal(IReadOnlyDictionary<string, string> p, string key, decimal fallback) =>
        p.TryGetValue(key, out var raw) && decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : fallback;
}
