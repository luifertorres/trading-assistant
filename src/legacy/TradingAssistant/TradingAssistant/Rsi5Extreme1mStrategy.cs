using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant;

internal sealed class Rsi5Extreme1mStrategy(ISender sender, ILogger<Rsi5Extreme1mStrategy> logger)
    : INotificationHandler<SmasAndRsisCalculatedEvent>
{
    private const KlineInterval TargetInterval = KlineInterval.OneMinute;
    private const int RsiIndexFor5 = 0;

    public async Task Handle(SmasAndRsisCalculatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.LastCandle.Interval != TargetInterval)
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

        if (previous < 10 && current > 10)
        {
            logger.LogInformation(
                "RSI(5) cross up 10 detected for {Symbol}. Sending long entry to TradeHandler",
                symbol);

            await sender.Send(new TradeRequest(symbol,
                    TargetInterval,
                    time,
                    PositionSide.Long,
                    OrderSide.Buy,
                    price),
                cancellationToken);

        }
    }
}

