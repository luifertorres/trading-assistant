namespace TradingAssistant.Application;

public interface ICandleRepository
{
    List<Candle> GetLastCandles(CandleId lastCandleId, int count);
    void UpsertMany(IEnumerable<(CandleId id, Candle candle)> candles);
}


