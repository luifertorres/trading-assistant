using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant;

/// <summary>
/// RSI(5) cross up through 10, gated by Ivan Scherman's SMA200/SMA5 entry filters
/// (ES1, 1D — Rankia Markets experience), mirrored for long: SMA200 uptrend, bearish candle, high below SMA5.
/// </summary>
internal sealed class Rsi5Extreme1mStrategy(ISender sender, ILogger<Rsi5Extreme1mStrategy> logger)
    : INotificationHandler<SmasAndRsisCalculatedEvent>
{
    private const KlineInterval TargetInterval = KlineInterval.OneMinute;
    private const int RsiIndexFor5 = 0;
    private const int SmaIndexFor5 = 0;
    private const int SmaIndexFor200 = 5;

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
        if (previous >= 10 || current <= 10)
        {
            return;
        }

        var smas = notification.Smas;
        if (smas.Length <= SmaIndexFor200
            || smas[SmaIndexFor5].Length < 1
            || smas[SmaIndexFor200].Length < 2)
        {
            return;
        }

        var candle = notification.LastCandle;
        var symbol = candle.Symbol;
        var price = candle.ClosePrice;
        var time = candle.OpenTime;
        var ma5 = (decimal)smas[SmaIndexFor5][^1];
        var isMa200Uptrending = new[] { smas[SmaIndexFor200] }.AreUptrending();
        var isBearishCandle = candle.ClosePrice < candle.OpenPrice;
        var isBelowMa5 = candle.HighPrice < ma5;

        if (!isMa200Uptrending || !isBearishCandle || !isBelowMa5)
        {
            logger.LogInformation(
                "RSI(5) cross up 10 for {Symbol} skipped: SMA200 uptrend={Sma200Uptrend}, bearish={Bearish}, belowMa5={BelowMa5}",
                symbol,
                isMa200Uptrending,
                isBearishCandle,
                isBelowMa5);
            return;
        }

        logger.LogInformation(
            "RSI(5) cross up 10 with Scherman SMA200/SMA5 long entry filters for {Symbol}. Sending long entry to TradeHandler",
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

