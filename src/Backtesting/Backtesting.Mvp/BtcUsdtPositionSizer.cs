namespace Backtesting.Mvp;

/// <summary>
/// Binance USDT-M BTCUSDT-style floors: min notional, min qty, lot step (all applied to quote/base as documented for the symbol).
/// </summary>
public static class BtcUsdtPositionSizer
{
    public static decimal QuantityForLong(decimal entryPrice, BacktestConfig config)
    {
        var targetNotional = Math.Max(
            config.InitialCapital * config.PositionNotionalFractionOfInitial,
            config.MinNotionalUsd);

        var rawQty = targetNotional / entryPrice;
        var steps = Math.Ceiling(rawQty / config.QuantityStepBtc);
        var quantity = steps * config.QuantityStepBtc;

        var minSteps = Math.Ceiling(config.MinOrderQuantityBtc / config.QuantityStepBtc);
        var minByLot = minSteps * config.QuantityStepBtc;
        quantity = Math.Max(quantity, minByLot);

        while (quantity * entryPrice < targetNotional)
            quantity += config.QuantityStepBtc;

        return quantity;
    }
}
