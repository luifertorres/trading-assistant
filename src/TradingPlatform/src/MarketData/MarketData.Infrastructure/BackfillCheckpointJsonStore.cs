using System.Text.Json;
using MarketData.Application;

namespace MarketData.Infrastructure;

public sealed class BackfillCheckpointJsonStore(string filePath) : IBackfillCheckpointStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<BackfillCheckpointDocumentV1?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer
            .DeserializeAsync<BackfillCheckpointDocumentV1>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(BackfillCheckpointDocumentV1 document, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
