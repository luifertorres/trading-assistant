using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;

namespace TradingAssistant;

internal sealed class Rsi5ExtremeStrategy(ISender sender,
    ILogger<Rsi5ExtremeStrategy> logger,
    BinanceService binance,
    ICandleRepository candleRepository)
    : INotificationHandler<SmasAndRsisCalculatedEvent>
{
    private const KlineInterval TargetInterval = KlineInterval.FourHour;
    private const int RsiIndexFor5 = 0;

    public async Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.LastCandle.Interval != TargetInterval)
        {
            return;
        }

        if (SymbolExclusions.Contains(notification.LastCandle.Symbol))
        {
            return;
        }

        var rsis = notification.Rsis;
        if (rsis.Length <= RsiIndexFor5 || rsis[RsiIndexFor5].Length < 2)
        {
            return;
        }

        var current = rsis[RsiIndexFor5][^1];
        var previous = rsis[RsiIndexFor5][^2];
        var symbol = notification.LastCandle.Symbol;
        var price = notification.LastCandle.ClosePrice;
        var time = notification.LastCandle.OpenTime;

        // Entry: Cross Up 10 (Prev < 10, Curr > 10)
        // Only this condition is kept as per requirements (Cross above 10)
        if (previous < 10 && current > 10)
        {
            if (!binance.TryGetLeverage(symbol, out var leverage))
            {
                logger.LogWarning("Could not get leverage for {Symbol}", symbol);
                return;
            }

            var candleId = new CandleId(symbol, TargetInterval, time);
            // Get last 6 candles (24 hours for 4H interval) including the current closed one?
            // "al cierre de la vela" means the current candle just closed.
            // "últimas 24 horas" -> last 6 candles including this one.
            var lastCandles = candleRepository.GetLastCandles(candleId, 6);

            if (lastCandles.Count == 0)
            {
                logger.LogWarning("Could not get last candles for {Symbol}", symbol);
                return;
            }

            var slPrice = lastCandles.Min(c => c.LowPrice);

            // 100% ROI:
            // ROI = (Exit - Entry) / Entry * Leverage
            // 1 = (TP - Price) / Price * Leverage
            // 1/Leverage = TP/Price - 1
            // TP/Price = 1 + 1/Leverage
            // TP = Price * (1 + 1/Leverage)
            var tpPrice = price * (1 + 1.0m / leverage);

            logger.LogInformation("RSI(5) Cross Up 10 detected for {Symbol}. Placing Entry 10% with SL: {SL}, TP: {TP}", symbol, slPrice, tpPrice);
            await PlaceEntry(symbol, time, price, 0.10m, slPrice, tpPrice, cancellationToken);
        }
    }

    private async Task PlaceEntry(string symbol, DateTime time, decimal price, decimal marginPct, decimal slPrice, decimal tpPrice, CancellationToken ct)
    {
        var tradeRequest = new TradeRequest(symbol,
            TargetInterval,
            time,
            PositionSide.Long,
            OrderSide.Buy,
            price,
            MarginPercentage: marginPct,
            IsPyramidingAllowed: false, // Single buy
            IsStopLossDisabled: false,
            StopLossPrice: slPrice,
            TakeProfitPrice: tpPrice);

        await sender.Send(tradeRequest, ct);
    }
}
