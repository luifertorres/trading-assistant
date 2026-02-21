using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface ICandlestickRepository
{
    Task<IReadOnlyList<CandlestickRecord>> GetCandlesAsync(
        IEnumerable<string> symbols,
        TimeFrame timeFrame,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task UpsertManyAsync(
        IEnumerable<CandlestickRecord> candles,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLatestOpenTimeAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DateTime From, DateTime To)>> FindMissingRangesAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
