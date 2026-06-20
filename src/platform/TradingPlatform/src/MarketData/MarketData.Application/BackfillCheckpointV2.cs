using System.Text.Json.Serialization;

namespace MarketData.Application;

public sealed class BackfillCheckpointDocumentV2
{
    public int SchemaVersion { get; set; } = 2;

    public Guid RunId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Full normalized path; resume only when it matches the current run.</summary>
    public string MarketDatabasePath { get; set; } = "";

    public List<BackfillCheckpointInstrumentEntryV2> Instruments { get; set; } = [];
}

public sealed class BackfillCheckpointInstrumentEntryV2
{
    public long InstrumentId { get; set; }

    public bool Complete { get; set; }

    public long? LastWrittenOpenTimeMs { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastErrorMessage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? LastErrorAtUtc { get; set; }
}
