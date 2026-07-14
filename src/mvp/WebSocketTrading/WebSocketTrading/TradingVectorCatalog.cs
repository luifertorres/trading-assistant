namespace WebSocketTrading;

public static class TradingVectorCatalog
{
    public static TradingVectorPlan Build(IReadOnlyList<TradingVector> vectors)
    {
        if (vectors.Count == 0)
            throw new InvalidOperationException("At least one trading vector must be configured.");

        var asset = vectors[0].Asset;

        foreach (var vector in vectors)
        {
            if (!string.Equals(vector.Asset, asset, StringComparison.Ordinal))
                throw new InvalidOperationException("All trading vectors must share the same Asset.");
        }

        var seen = new HashSet<TradingVector>();
        foreach (var vector in vectors)
        {
            if (!seen.Add(vector))
                throw new InvalidOperationException(
                    "Each trading vector must be unique (Asset, Direction, Timeframe, TradingLogic).");
        }

        var distinctTimeframes = vectors
            .Select(v => v.Timeframe)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new TradingVectorPlan(asset, vectors, distinctTimeframes);
    }
}
