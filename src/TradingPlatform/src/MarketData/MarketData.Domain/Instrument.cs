using TradingPlatform.Kernel;

namespace MarketData.Domain;

public sealed class Instrument
{
    public InstrumentId Id { get; init; }

    public string Venue { get; init; } = "";

    public string Market { get; init; } = "";

    public string ContractType { get; init; } = "";

    public string ExchangeSymbol { get; init; } = "";

    public string BaseAsset { get; init; } = "";

    public string QuoteAsset { get; init; } = "";

    public string Pair { get; init; } = "";

    public int PricePrecision { get; init; }

    public int QuantityPrecision { get; init; }

    public string FiltersJson { get; init; } = "";

    public DateTimeOffset FirstSeenUtc { get; init; }

    public DateTimeOffset LastSeenUtc { get; init; }

    public string LastStatus { get; init; } = "";
}
