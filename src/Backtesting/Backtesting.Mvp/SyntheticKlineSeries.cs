using Binance.Net.Interfaces;

namespace Backtesting.Mvp;

/// <summary>Deterministic mock klines for demos and tests (ascending open time).</summary>
public static class SyntheticKlineSeries
{
    public static IReadOnlyList<IBinanceKline> OscillatingUsd(
        int count,
        DateTime startUtc,
        TimeSpan barDuration,
        decimal basePrice = 100m,
        decimal amplitude = 8m)
    {
        var list = new List<IBinanceKline>(count);
        var prevClose = basePrice;
        for (var i = 0; i < count; i++)
        {
            var openTime = startUtc + barDuration * i;
            var closeTime = openTime + barDuration;
            var w = (decimal)(Math.Sin(i * 0.22) + Math.Sin(i * 0.07));
            var close = basePrice + amplitude * w;
            var high = close + 0.5m;
            var low = close - 0.5m;
            var open = prevClose;
            prevClose = close;
            list.Add(MockBinanceKline.Create(openTime, closeTime, open, high, low, close));
        }

        return list;
    }

    /// <summary>
    /// Chained dump/rally segments tuned so RSI(14) repeatedly crosses below oversold then above overbought,
    /// yielding roughly one long round-trip per cycle after warmup (for the default engine thresholds 30/70).
    /// </summary>
    public static IReadOnlyList<IBinanceKline> MeanReversionLongCycles(
        int cycles,
        DateTime startUtc,
        TimeSpan barDuration,
        decimal dumpDelta = 16m,
        decimal rallyDelta = 22m,
        int barsPerDump = 26,
        int barsPerRally = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cycles);

        var list = new List<IBinanceKline>(cycles * (barsPerDump + barsPerRally));
        var prevClose = 100m;
        var barIndex = 0;

        for (var c = 0; c < cycles; c++)
        {
            var dumpEnd = prevClose - dumpDelta;
            AppendLinearSegment(list, ref prevClose, ref barIndex, startUtc, barDuration, barsPerDump, dumpEnd);

            var rallyEnd = dumpEnd + rallyDelta;
            AppendLinearSegment(list, ref prevClose, ref barIndex, startUtc, barDuration, barsPerRally, rallyEnd);
        }

        return list;
    }

    private static void AppendLinearSegment(
        List<IBinanceKline> list,
        ref decimal prevClose,
        ref int barIndex,
        DateTime startUtc,
        TimeSpan barDuration,
        int bars,
        decimal segmentEndClose)
    {
        var start = prevClose;
        for (var i = 0; i < bars; i++)
        {
            var close = bars == 1
                ? segmentEndClose
                : start + (segmentEndClose - start) * (i + 1) / bars;

            var open = prevClose;
            var high = decimal.Max(open, close) + 0.25m;
            var low = decimal.Min(open, close) - 0.25m;
            var openTime = startUtc + barDuration * barIndex;
            var closeTime = openTime + barDuration;
            list.Add(MockBinanceKline.Create(openTime, closeTime, open, high, low, close));
            prevClose = close;
            barIndex++;
        }
    }
}
