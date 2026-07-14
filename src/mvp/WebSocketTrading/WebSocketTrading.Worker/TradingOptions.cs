using Binance.Net.Enums;

namespace WebSocketTrading.Worker;

public sealed class TradingOptions
{
    public const string SectionName = "Trading";

    public decimal NotionalUsd { get; init; } = 5m;

    public int Leverage { get; init; } = 1;

    public IReadOnlyList<TradingVectorOptions> Vectors { get; init; } =
    [
        new TradingVectorOptions
        {
            Asset = "DOGEUSDT",
            Direction = WebSocketTrading.Direction.Short,
            Timeframe = "OneDay",
            TradingLogic = WebSocketTrading.TradingLogicKind.Sma200Sma5
        }
    ];
}

public sealed class TradingVectorOptions
{
    public string Asset { get; init; } = "DOGEUSDT";

    public WebSocketTrading.Direction Direction { get; init; } = WebSocketTrading.Direction.Short;

    public string Timeframe { get; init; } = "OneDay";

    public WebSocketTrading.TradingLogicKind TradingLogic { get; init; } =
        WebSocketTrading.TradingLogicKind.Sma200Sma5;

    public KlineInterval GetKlineInterval() =>
        Enum.Parse<KlineInterval>(Timeframe, ignoreCase: true);
}
