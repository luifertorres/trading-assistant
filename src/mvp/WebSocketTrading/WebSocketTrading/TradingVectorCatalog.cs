namespace WebSocketTrading;

public static class TradingVectorCatalog
{
    public static TradingVectorPlan Build(IReadOnlyList<TradingVector> vectors)
    {
        if (vectors.Count == 0)
            throw new InvalidOperationException("At least one trading vector must be configured.");

        var seen = new HashSet<TradingVector>();
        foreach (var vector in vectors)
        {
            if (!seen.Add(vector))
                throw new InvalidOperationException(
                    "Each trading vector must be unique (Asset, Direction, Timeframe, TradingLogic).");
        }

        var assets = vectors
            .Select(v => v.Asset)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var distinctTimeframes = vectors
            .Select(v => v.Timeframe)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var distinctAssetTimeframes = vectors
            .Select(v => new AssetTimeframe(v.Asset, v.Timeframe))
            .Distinct()
            .OrderBy(x => x.Asset, StringComparer.Ordinal)
            .ThenBy(x => x.Timeframe, StringComparer.Ordinal)
            .ToList();

        return new TradingVectorPlan(assets, vectors, distinctTimeframes, distinctAssetTimeframes);
    }
}
