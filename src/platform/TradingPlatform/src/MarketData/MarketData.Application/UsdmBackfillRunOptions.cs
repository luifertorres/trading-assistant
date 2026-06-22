using TradingPlatform.Kernel;

namespace MarketData.Application;

/// <summary>Broker-agnostic options for USD-M candle backfill.</summary>
public sealed record UsdmBackfillRunOptions(
    string MarketDatabasePath,
    string DataRoot,
    string? CheckpointFilePath,
    bool WriteExchangeInfoSnapshot,
    TimeFrameCode TimeFrame,
    IReadOnlyList<string>? SymbolFilter = null);

/// <summary>Backward-compatible alias for 1d backfill.</summary>
public sealed record Usdm1dBackfillRunOptions(
    string MarketDatabasePath,
    string DataRoot,
    string? CheckpointFilePath,
    bool WriteExchangeInfoSnapshot)
{
    public UsdmBackfillRunOptions ToGeneral() =>
        new(MarketDatabasePath, DataRoot, CheckpointFilePath, WriteExchangeInfoSnapshot, TimeFrameCode.Day1);
}
