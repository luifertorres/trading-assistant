using System.Globalization;
using Research.Application;
using Skender.Stock.Indicators;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

/// <summary>
/// RSI(5) cross up through 10; SL = min low of last 6 bars (24h on 4H); TP = entry * (1 + takeProfitPct).
/// Parameters: takeProfitPct (default 0.08), rsiExit (default 70), lookbackBars (default 6).
/// With <see cref="RequireSma200DirectionBias"/>: SMA200 slope gate, no hard SL/TP, condition exits only.
/// </summary>
public sealed class Rsi5ExtremeStrategy(bool requireSma200DirectionBias = false) : ITradingStrategy
{
    private const int SmaPeriod = 200;
    private readonly List<OhlcBar> _bars = [];
    private bool _inPosition;
    private decimal _stopLoss;
    private decimal _takeProfit;

    public void OnBar(BarProcessingContext context)
    {
        _bars.Add(context.Bar);
        var takeProfitPct = ParseDecimal(context.Vector.Parameters, "takeProfitPct", 0.08m);
        var rsiExit = ParseDecimal(context.Vector.Parameters, "rsiExit", 70m);
        var rsiEntryShort = ParseDecimal(context.Vector.Parameters, "rsiEntryShort", 90m);
        var rsiExitShort = ParseDecimal(context.Vector.Parameters, "rsiExitShort", 30m);
        var lookback = int.Parse(context.Vector.Parameters.GetValueOrDefault("lookbackBars", "6"), CultureInfo.InvariantCulture);

        if (_inPosition)
        {
            TryExit(context, rsiExit, rsiExitShort);
            return;
        }

        if (requireSma200DirectionBias)
        {
            TryEnterWithBias(context, rsiExitShort, rsiEntryShort);
            return;
        }

        if (_bars.Count < lookback + 2)
            return;

        if (context.Vector.Direction != Direction.Long)
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

    private void TryEnterWithBias(BarProcessingContext context, decimal rsiExitShort, decimal rsiEntryShort)
    {
        if (_bars.Count < SmaPeriod + 2)
            return;

        var (ma200, ma200Prev) = GetSma200(_bars.Count - 1);
        if (ma200 is null || ma200Prev is null)
            return;

        var rsiNow = GetRsi(_bars.Count - 1);
        var rsiPrev = GetRsi(_bars.Count - 2);

        if (context.Vector.Direction == Direction.Long
            && ma200Prev < ma200
            && rsiPrev < 10m
            && rsiNow > 10m)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.OpenLong, 0, "rsi5-cross-sma200"), context.Bar);
            _inPosition = true;
            return;
        }

        if (context.Vector.Direction == Direction.Short
            && ma200Prev > ma200
            && rsiPrev > rsiEntryShort
            && rsiNow < rsiEntryShort)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.OpenShort, 0, "rsi5-cross-sma200"), context.Bar);
            _inPosition = true;
        }
    }

    private void TryExit(BarProcessingContext context, decimal rsiExit, decimal rsiExitShort)
    {
        var bar = context.Bar;

        if (requireSma200DirectionBias)
        {
            var rsi = GetRsi(_bars.Count - 1);
            if (context.Vector.Direction == Direction.Long && rsi >= rsiExit)
            {
                context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "rsi-exit"), bar);
                _inPosition = false;
                return;
            }

            if (context.Vector.Direction == Direction.Short && rsi <= rsiExitShort)
            {
                context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "rsi-exit"), bar);
                _inPosition = false;
            }

            return;
        }

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

        var rsiLong = GetRsi(_bars.Count - 1);
        if (rsiLong >= rsiExit)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "rsi-exit"), bar);
            _inPosition = false;
        }
    }

    private (double? Current, double? Previous) GetSma200(int barIndex)
    {
        if (barIndex < SmaPeriod)
            return (null, null);

        var quotes = _bars
            .Take(barIndex + 1)
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

        var sma = quotes.GetSma(SmaPeriod).ToList();
        var last = sma[barIndex].Sma;
        var prev = sma[barIndex - 1].Sma;
        return (last, prev);
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
