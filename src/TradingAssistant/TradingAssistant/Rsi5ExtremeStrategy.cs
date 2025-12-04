using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using TradingAssistant.Application;
using ApplicationIndicatorType = TradingAssistant.Application.IndicatorType;

namespace TradingAssistant;

internal sealed class Rsi5ExtremeStrategy(ISender sender,
    ILogger<Rsi5ExtremeStrategy> logger,
    BinanceService binance)
    : INotificationHandler<IndicatorConditionMetNotification>,
      INotificationHandler<SmasAndRsisCalculatedEvent>
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

        // Entry 1: Cross Down 10 (Prev > 10, Curr < 10)
        if (previous > 10 && current < 10)
        {
            logger.LogInformation("RSI(5) Cross Down 10 detected for {Symbol}. Placing Entry 5%.", symbol);
            await PlaceEntry(symbol, time, price, 0.05m, cancellationToken);
        }
        // Entry 2: Cross Up 10 (Prev < 10, Curr > 10)
        else if (previous < 10 && current > 10)
        {
            logger.LogInformation("RSI(5) Cross Up 10 detected for {Symbol}. Placing Entry 10%.", symbol);
            await PlaceEntry(symbol, time, price, 0.10m, cancellationToken);
        }
    }

    public async Task Handle(IndicatorConditionMetNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Indicator != ApplicationIndicatorType.Rsi || notification.Period != 5)
        {
            return;
        }

        if (SymbolExclusions.Contains(notification.Symbol))
        {
            return;
        }

        // Only handle RealTime (Exit/BE) logic here.
        if (notification.Source != IndicatorEventSource.RealTime)
        {
            return;
        }

        // Close Position (RSI >= 90)
        if (notification.Threshold >= 90.0)
        {
            logger.LogInformation("RSI(5) >= 90 ({Value:F2}) for {Symbol}. Closing Position.", notification.Value, notification.Symbol);
            await sender.Send(new ClosePositionRequest(notification.Symbol), cancellationToken);
        }
        // Move SL to BE (RSI >= 50)
        else if (notification.Threshold >= 50.0)
        {
            logger.LogInformation("RSI(5) >= 50 ({Value:F2}) for {Symbol}. Moving SL to BE.", notification.Value, notification.Symbol);
            await MoveStopLossToBreakeven(notification.Symbol, cancellationToken);
        }
    }

    private async Task PlaceEntry(string symbol, DateTime time, decimal price, decimal marginPct, CancellationToken ct)
    {
        var tradeRequest = new TradeRequest(symbol,
            TargetInterval,
            time,
            PositionSide.Long,
            OrderSide.Buy,
            price,
            MarginPercentage: marginPct,
            IsPyramidingAllowed: true,
            IsStopLossDisabled: true);

        await sender.Send(tradeRequest, ct);
    }

    private async Task MoveStopLossToBreakeven(string symbol, CancellationToken ct)
    {
        var account = await binance.TryGetAccountInformationAsync(ct);
        var position = account?.Positions.FirstOrDefault(p => p.Symbol == symbol && p.Quantity != 0);

        if (position == null || position.EntryPrice <= 0)
        {
            return;
        }

        // Cancel existing SL first
        await binance.TryCancelStopLossAsync(symbol, ct);

        // Place BE Stop Market order (Close Position at Entry Price)
        // Assuming Long position (Buy to Open, Sell to Close).
        // TryPlaceBreakEvenAsync takes positionSide (OrderSide). It reverses it internally.
        // Since we are Long (Buy), we pass OrderSide.Buy.
        await binance.TryPlaceBreakEvenAsync(symbol, OrderSide.Buy, position.EntryPrice, ct);
    }
}
