namespace WebSocketTrading;

public static class TradingUniverseFactory
{
    public static IReadOnlyList<TradingVector> BuildLongShort(
        IEnumerable<string> symbols,
        string timeframe,
        TradingLogic tradingLogic)
    {
        var symbolList = symbols
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (symbolList.Count == 0)
            throw new InvalidOperationException("At least one symbol must be provided.");

        var vectors = new List<TradingVector>(symbolList.Count * 2);
        foreach (var symbol in symbolList)
        {
            vectors.Add(new TradingVector(symbol, Direction.Short, timeframe, tradingLogic));
            vectors.Add(new TradingVector(symbol, Direction.Long, timeframe, tradingLogic));
        }

        return vectors;
    }
}
