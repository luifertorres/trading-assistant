using System;
using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant.Application;

public enum IndicatorType
{
    Rsi,
}

public enum IndicatorComparison
{
    LessThanOrEqual,
    GreaterThanOrEqual,
}

public enum IndicatorEventSource
{
    CandleClose,
    RealTime,
}

public record IndicatorConditionMetNotification(string Symbol,
    KlineInterval Interval,
    DateTime OpenTime,
    decimal Price,
    IndicatorType Indicator,
    int Period,
    double Value,
    double Threshold,
    IndicatorComparison Comparison,
    IndicatorEventSource Source) : INotification;


