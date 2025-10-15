using MediatR;

namespace TradingAssistant.Application;

public record SmasAndRsisCalculatedEvent(Candle LastCandle,
    double[][] SmasHigherTimeFrame,
    double[][] Smas,
    double[][] Rsis) : INotification;


