using MediatR;

namespace TradingAssistant.Application;

public record CandleClosedNotification(CandleId CandleId) : INotification;


