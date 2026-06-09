namespace MarketData.Application;

public sealed record UsdMInstrumentListing(InstrumentUpsert Upsert, BrokerFetchHandle FetchHandle);
