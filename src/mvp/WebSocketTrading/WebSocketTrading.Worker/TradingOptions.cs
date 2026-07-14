using Binance.Net.Enums;

namespace WebSocketTrading.Worker;

public sealed class TradingOptions
{
    public const string SectionName = "Trading";

    public string Symbol { get; init; } = "DOGEUSDT";

    public string Interval { get; init; } = "OneDay";

    public decimal NotionalUsd { get; init; } = 5m;

    public int Leverage { get; init; } = 1;

    public KlineInterval GetKlineInterval() =>
        Enum.Parse<KlineInterval>(Interval, ignoreCase: true);
}
