namespace MarketData.Application;

public sealed class BackfillCheckpointDocumentV1
{
    public int SchemaVersion { get; set; } = 1;

    public Guid RunId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Full normalized path; resume only when it matches the current run.</summary>
    public string MarketDatabasePath { get; set; } = "";

    public List<BackfillCheckpointSymbolEntryV1> Symbols { get; set; } = [];
}

public sealed class BackfillCheckpointSymbolEntryV1
{
    public string Symbol { get; set; } = "";

    public bool Complete { get; set; }

    public long? LastWrittenOpenTimeMs { get; set; }
}
