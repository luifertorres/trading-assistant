namespace MarketData.Application;

/// <summary>Opaque broker token for kline fetch; unwrap only in MarketData.Infrastructure.</summary>
public readonly record struct BrokerFetchHandle(object Token);
