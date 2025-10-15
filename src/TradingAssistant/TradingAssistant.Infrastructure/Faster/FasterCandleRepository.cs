using FASTER.core;
using Microsoft.Extensions.Logging;
using TradingAssistant.Application;
using TradingAssistant.Infrastructure.Faster;

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

        var timeFrameInSeconds = (long)lastCandleId.TimeFrame;
        var lastOpenTime = lastCandleId.OpenTime;
        var lastSlot = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.FromUnixTimeMilliseconds(lastOpenTime.Ticks / TimeSpan.TicksPerMillisecond).ToUnixTimeSeconds()).UtcDateTime;
        var remaining = count - 1;

        foreach (var openTime in Enumerable.Repeat(lastOpenTime, count)
                     .Select(_ => lastSlot.AddSeconds(-(timeFrameInSeconds * remaining--))))
        {
            var key = new CandleId(lastCandleId.Symbol, lastCandleId.TimeFrame, openTime);
            var value = default(Candle);
            var status = session.Read(ref key, ref value);
            if (status.Found)
            {
                result.Add(value);
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


