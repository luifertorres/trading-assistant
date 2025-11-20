using System;
using System.Collections.Concurrent;
using Binance.Net.Enums;
using MediatR;
using TradingAssistant.Application;
using ApplicationIndicatorType = TradingAssistant.Application.IndicatorType;

namespace TradingAssistant;

internal sealed class Rsi5ExtremeStrategy(IPublisher publisher,
    ISender sender,
    ILogger<Rsi5ExtremeStrategy> logger)
    : INotificationHandler<IndicatorConditionMetNotification>
{
    private const KlineInterval TargetInterval = KlineInterval.FourHour;
    private const double EntryThreshold = 10.0;
    private const double ExitThreshold = 90.0;
    private readonly ConcurrentDictionary<string, byte> _managedSymbols = new(StringComparer.OrdinalIgnoreCase);

    public async Task Handle(IndicatorConditionMetNotification notification, CancellationToken cancellationToken)
    {
        if (!IsRsi5(notification) || SymbolExclusions.Contains(notification.Symbol))
        {
            return;
        }

        if (IsEntrySignal(notification))
        {
            _managedSymbols.AddOrUpdate(notification.Symbol, _ => 0, (_, _) => 0);

            var tradingSignal = new TradingSignalNotification(notification.Symbol,
                notification.Interval,
                notification.OpenTime,
                PositionSide.Long,
                OrderSide.Buy,
                notification.Price);

            logger.LogInformation("RSI(5) entry signal detected for {Symbol} at {Price}",
                notification.Symbol,
                notification.Price);

            await publisher.Publish(tradingSignal, cancellationToken);

            return;
        }

        if (IsExitSignal(notification) && _managedSymbols.ContainsKey(notification.Symbol))
        {
            _managedSymbols.TryRemove(notification.Symbol, out _);

            logger.LogInformation("RSI(5) exit signal detected for {Symbol} (value: {Value:F2})",
                notification.Symbol,
                notification.Value);

            await sender.Send(new ClosePositionRequest(notification.Symbol), cancellationToken);
        }
    }

    private static bool IsRsi5(IndicatorConditionMetNotification notification)
    {
        return notification.Indicator == ApplicationIndicatorType.Rsi && notification.Period == 5;
    }

    private static bool IsEntrySignal(IndicatorConditionMetNotification notification)
    {
        return notification.Interval == TargetInterval
            && notification.Source == IndicatorEventSource.CandleClose
            && notification.Comparison == IndicatorComparison.LessThanOrEqual
            && notification.Threshold <= EntryThreshold;
    }

    private static bool IsExitSignal(IndicatorConditionMetNotification notification)
    {
        return notification.Source == IndicatorEventSource.RealTime
            && notification.Comparison == IndicatorComparison.GreaterThanOrEqual
            && notification.Threshold >= ExitThreshold;
    }
}


