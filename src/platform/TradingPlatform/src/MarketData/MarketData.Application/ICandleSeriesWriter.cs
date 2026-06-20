using TradingPlatform.Kernel;

namespace MarketData.Application;

public interface ICandleSeriesWriter
{
    Task UpsertAsync(SeriesDescriptor series, IReadOnlyList<OhlcBar> bars, CancellationToken cancellationToken = default);
}
