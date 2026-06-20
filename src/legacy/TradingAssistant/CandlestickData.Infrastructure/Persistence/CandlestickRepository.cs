using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class CandlestickRepository(CandlestickDataContext context) : ICandlestickRepository
{
    public async Task<IReadOnlyList<CandlestickRecord>> GetCandlesAsync(
        IEnumerable<string> symbols,
        TimeFrame timeFrame,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();

        return await context.Candlesticks
            .AsNoTracking()
            .Where(c => symbolList.Contains(c.Symbol)
                && c.TimeFrame == timeFrame
                && c.OpenTime >= from
                && c.OpenTime <= to)
            .OrderBy(c => c.OpenTime)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertManyAsync(
        IEnumerable<CandlestickRecord> candles,
        CancellationToken cancellationToken = default)
    {
        foreach (var candle in candles)
        {
            var existing = await context.Candlesticks
                .FindAsync([candle.Symbol, candle.TimeFrame, candle.OpenTime], cancellationToken);

            if (existing is null)
                context.Candlesticks.Add(candle);
            else
                context.Entry(existing).CurrentValues.SetValues(candle);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLatestOpenTimeAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default)
    {
        return await context.Candlesticks
            .AsNoTracking()
            .Where(c => c.Symbol == symbol && c.TimeFrame == timeFrame)
            .MaxAsync(c => (DateTime?)c.OpenTime, cancellationToken);
    }

    public async Task<IReadOnlyList<(DateTime From, DateTime To)>> FindMissingRangesAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var interval = timeFrame.ToTimeSpan();

        var openTimes = await context.Candlesticks
            .AsNoTracking()
            .Where(c => c.Symbol == symbol
                && c.TimeFrame == timeFrame
                && c.OpenTime >= from
                && c.OpenTime <= to)
            .Select(c => c.OpenTime)
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);

        var missingRanges = new List<(DateTime From, DateTime To)>();
        var expected = from;

        foreach (var actual in openTimes)
        {
            if (actual > expected)
                missingRanges.Add((expected, actual));

            expected = actual + interval;
        }

        if (expected < to)
            missingRanges.Add((expected, to));

        return missingRanges;
    }
}
