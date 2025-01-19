using MediatR;

namespace TradingAssistant
{
    internal record SmasAndRsisCalculatedEvent(Candle LastCandle,
        double[][] SmasHigherTimeFrame,
        double[][] Smas,
        double[][] Rsis) : INotification;
}
