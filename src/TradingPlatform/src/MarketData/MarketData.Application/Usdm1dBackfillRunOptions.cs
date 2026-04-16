namespace MarketData.Application;

/// <summary>Broker-agnostic options for the USD-M 1d candle backfill. Rate limits and HTTP retries are handled by Binance.Net.</summary>
public sealed record Usdm1dBackfillRunOptions(
    string MarketDatabasePath,
    string DataRoot,
    string? CheckpointFilePath,
    bool WriteExchangeInfoSnapshot);
