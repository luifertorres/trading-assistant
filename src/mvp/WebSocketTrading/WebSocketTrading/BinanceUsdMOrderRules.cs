namespace WebSocketTrading;

public static class BinanceUsdMOrderRules
{
    /// <summary>
    /// Binance USD-M rejects reduceOnly in hedge mode when positionSide is set.
    /// Returns null so callers omit the parameter from PlaceOrderAsync.
    /// </summary>
    public static bool? ReduceOnlyParameter(bool hedgeMode) =>
        hedgeMode ? null : true;
}
