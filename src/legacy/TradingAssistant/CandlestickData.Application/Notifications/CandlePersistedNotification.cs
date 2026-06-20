using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Application.Notifications;

public record CandlePersistedNotification(
    string Symbol,
    TimeFrame TimeFrame,
    DateTime OpenTime) : INotification;
