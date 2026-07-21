namespace TradingPlatform.Kernel;

public static class TradingVectorIdentity
{
    public static void EnsureUnique(IReadOnlyList<TradingVectorSpec> vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);

        var seen = new HashSet<(Asset Asset, Direction Direction, TimeFrameCode TimeFrame, string TradingLogic)>();
        foreach (var vector in vectors)
        {
            var key = (vector.Asset, vector.Direction, vector.TimeFrame, vector.TradingLogic);
            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    "Each trading vector must be unique (Asset, Direction, TimeFrame, TradingLogic).");
            }
        }
    }
}
