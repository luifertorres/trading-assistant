using TradingPlatform.Kernel;

namespace MarketData.Application;

public interface ICandleSeriesReader
{
    Task<IReadOnlyList<OhlcBar>> ReadAsync(
        SeriesDescriptor series,
        DateTimeOffset? fromOpenTime,
        DateTimeOffset? toOpenTime,
        CancellationToken cancellationToken = default);
}
