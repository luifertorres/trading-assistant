using Binance.Net.Interfaces;
using Skender.Stock.Indicators;

namespace Backtesting.Mvp;

/// <summary>
/// Single-position USDT-M style long-only backtest: enter when RSI crosses up through oversold, exit when RSI crosses up through overbought.
/// Fills at bar close; fees per side in basis points on notional.
/// </summary>
public sealed class BacktestEngine(BacktestConfig config)
{
    private const int WarmupMultiplier = 20;

    public BacktestRunResult Run(IBacktestKlineSource source)
    {
        var klines = source.GetKlines();
        var quotes = new List<Quote>(klines.Count);
        foreach (var k in klines)
        {
            quotes.Add(new Quote
            {
                Date = k.OpenTime,
                Open = k.OpenPrice,
                High = k.HighPrice,
                Low = k.LowPrice,
                Close = k.ClosePrice,
                Volume = k.Volume
            });
        }

        var rsiResults = quotes.Validate().GetRsi(config.RsiPeriod).ToList();
        var trades = new List<TradeRecord>();
        var equitySeries = new List<EquityPoint>(klines.Count);

        var cash = config.InitialCapital;
        decimal? entryPrice = null;
        decimal? entryQuantity = null;
        DateTime? entryTime = null;
        var entryFeePaid = 0m;
        var minBars = Math.Max(config.RsiPeriod * WarmupMultiplier, config.RsiPeriod + 2);

        for (var i = 0; i < klines.Count; i++)
        {
            var k = klines[i];

            decimal MarkEquity()
            {
                if (entryPrice is null || entryQuantity is null)
                    return cash;
                var unrealized = entryQuantity.Value * (k.ClosePrice - entryPrice.Value);
                return cash + unrealized;
            }

            equitySeries.Add(new EquityPoint(k.CloseTime, MarkEquity()));

            if (i < minBars - 1)
                continue;

            var prevRsi = rsiResults[i - 1].Rsi;
            var currRsi = rsiResults[i].Rsi;
            if (!prevRsi.HasValue || !currRsi.HasValue)
                continue;

            var prev = (decimal)prevRsi.Value;
            var curr = (decimal)currRsi.Value;

            if (entryPrice is null
                && prev < config.RsiOversold
                && curr >= config.RsiOversold)
            {
                var fill = k.ClosePrice;
                var qty = config.SizePositionByInitialCapitalFraction
                    ? BtcUsdtPositionSizer.QuantityForLong(fill, config)
                    : config.Quantity;
                entryFeePaid = qty * fill * (config.FeeBpsPerSide / 10_000m);
                cash -= entryFeePaid;
                entryPrice = fill;
                entryQuantity = qty;
                entryTime = k.CloseTime;
            }
            else if (entryPrice is not null
                     && entryQuantity is not null
                     && prev < config.RsiOverbought
                     && curr >= config.RsiOverbought)
            {
                var exitPrice = k.ClosePrice;
                var qty = entryQuantity.Value;
                var grossPnl = qty * (exitPrice - entryPrice.Value);
                var exitFee = qty * exitPrice * (config.FeeBpsPerSide / 10_000m);
                cash += grossPnl - exitFee;
                var totalFees = entryFeePaid + exitFee;
                var netPnl = grossPnl - totalFees;
                trades.Add(new TradeRecord(
                    entryTime!.Value,
                    k.CloseTime,
                    entryPrice.Value,
                    exitPrice,
                    qty,
                    grossPnl,
                    totalFees,
                    netPnl));
                entryPrice = null;
                entryQuantity = null;
                entryTime = null;
                entryFeePaid = 0m;
            }
        }

        return new BacktestRunResult(config, trades, equitySeries);
    }
}
