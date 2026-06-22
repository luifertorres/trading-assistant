using TradingPlatform.Kernel;

namespace MarketData.Application;

/// <summary>Closed-candle events for live strategy evaluation.</summary>
public sealed class ClosedCandleEvent(InstrumentId instrumentId, string exchangeSymbol, TimeFrameCode timeFrame, OhlcBar bar)
{
    public InstrumentId InstrumentId { get; } = instrumentId;
    public string ExchangeSymbol { get; } = exchangeSymbol;
    public TimeFrameCode TimeFrame { get; } = timeFrame;
    public OhlcBar Bar { get; } = bar;
}

public interface ILiveCandleFeed
{
    Task SubscribeAsync(
        IReadOnlyList<string> exchangeSymbols,
        TimeFrameCode timeFrame,
        Func<ClosedCandleEvent, CancellationToken, Task> onClosedCandle,
        CancellationToken cancellationToken = default);

    /// <summary>Fetch and emit the most recently closed bar per symbol (startup replay).</summary>
    Task ReplayLastClosedAsync(
        IReadOnlyList<string> exchangeSymbols,
        TimeFrameCode timeFrame,
        Func<ClosedCandleEvent, CancellationToken, Task> onClosedCandle,
        CancellationToken cancellationToken = default);
}
