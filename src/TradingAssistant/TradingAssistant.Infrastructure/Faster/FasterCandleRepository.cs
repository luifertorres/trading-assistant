using FASTER.core;
using Microsoft.Extensions.Logging;
using TradingAssistant.Application;

namespace TradingAssistant.Infrastructure.Faster;

public class FasterCandleRepository : ICandleRepository
{
    private readonly FasterKV<CandleId, Candle> _store;
    private readonly ILogger<FasterCandleRepository> _logger;

    public FasterCandleRepository(FasterKV<CandleId, Candle> store, ILogger<FasterCandleRepository> logger)
    {
        _store = store;
        _logger = logger;
    }

    public List<Candle> GetLastCandles(CandleId lastCandleId, int count)
    {
        var sessionBuilder = _store.For(new SimpleFunctions<CandleId, Candle>());
        var result = new List<Candle>(capacity: count);

        using var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>();

        var timeFrameInSeconds = lastCandleId.TimeFrame.ToSeconds();
        // Ensure we are working with clean seconds to avoid millisecond drift issues if any
        var lastOpenTime = lastCandleId.OpenTime;

        // We want candles from (Current - (count-1)) to Current
        for (var i = count - 1; i >= 0; i--)
        {
            var offsetSeconds = i * timeFrameInSeconds;
            var openTime = lastOpenTime.AddSeconds(-offsetSeconds);

            var key = new CandleId(lastCandleId.Symbol, lastCandleId.TimeFrame, openTime);
            var value = default(Candle);
            var status = session.Read(ref key, ref value);

            if (status.Found)
            {
                result.Add(value);
            }
            else
            {
                // Optional: Log missing candle if needed for debugging, but might be noisy
                // _logger.LogTrace("Candle not found: {Symbol} {Time}", key.Symbol, key.OpenTime);
            }
        }

        return result;
    }

    public void UpsertMany(IEnumerable<(CandleId id, Candle candle)> candles)
    {
        var sessionBuilder = _store.For(new SimpleFunctions<CandleId, Candle>());
        using var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>();
        foreach (var (id, candle) in candles)
        {
            var key = id; var val = candle;
            session.Upsert(ref key, ref val);
        }
    }
}
