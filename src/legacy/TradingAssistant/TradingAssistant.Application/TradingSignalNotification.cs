using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant.Application;

public record TradingSignalNotification(string Symbol,
    KlineInterval TimeFrame,
    DateTime Time,
    PositionSide Direction,
    OrderSide Side,
    decimal EntryPrice) : INotification;


