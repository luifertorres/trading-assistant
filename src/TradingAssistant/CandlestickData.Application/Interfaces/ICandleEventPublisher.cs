using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface ICandleEventPublisher
{
    Task PublishCandleClosedAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime openTime,
        CancellationToken cancellationToken = default);
}
