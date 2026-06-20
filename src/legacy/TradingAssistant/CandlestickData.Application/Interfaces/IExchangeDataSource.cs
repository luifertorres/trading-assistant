using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface IExchangeDataSource
{
    Task<IReadOnlyList<CandlestickRecord>> GetKlinesAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime startTime,
        DateTime? endTime,
        int limit,
        CancellationToken cancellationToken = default);

    Task SubscribeToKlineUpdatesAsync(
        IEnumerable<string> symbols,
        IEnumerable<TimeFrame> timeFrames,
        Func<CandlestickRecord, bool, Task> onKlineUpdate,
        CancellationToken cancellationToken = default);
}
