using System.Text.Json;
using MarketData.Application;
using Microsoft.Extensions.Logging;

namespace MarketData.Infrastructure;

public sealed class BackfillCheckpointJsonStore(string filePath, ILogger<BackfillCheckpointJsonStore> log) : IBackfillCheckpointStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<BackfillCheckpointDocumentV2?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement) ||
            versionElement.GetInt32() != 2)
        {
            log.LogWarning(
                "Checkpoint at {Path} is missing or not schema v2; starting fresh (delete v1 checkpoint or use a new path).",
                filePath);
            return null;
        }

        return JsonSerializer.Deserialize<BackfillCheckpointDocumentV2>(json, JsonOptions);
    }

    public async Task SaveAsync(BackfillCheckpointDocumentV2 document, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        document.SchemaVersion = 2;
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
