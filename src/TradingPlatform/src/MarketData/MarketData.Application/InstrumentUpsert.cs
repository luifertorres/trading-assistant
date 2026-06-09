namespace MarketData.Application;

public sealed record InstrumentUpsert(
    string Venue,
    string Market,
    string ContractType,
    string ExchangeSymbol,
    string BaseAsset,
    string QuoteAsset,
    string Pair,
    int PricePrecision,
    int QuantityPrecision,
    string FiltersJson,
    string LastStatus,
    DateTimeOffset SeenAtUtc);
