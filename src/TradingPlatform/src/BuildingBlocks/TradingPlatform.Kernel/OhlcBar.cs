namespace TradingPlatform.Kernel;

public readonly record struct OhlcBar(
    DateTimeOffset OpenTime,
    DateTimeOffset CloseTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
