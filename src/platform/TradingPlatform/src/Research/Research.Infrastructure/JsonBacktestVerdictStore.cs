using System.Text.Json;
using Research.Application;

namespace Research.Infrastructure;

public sealed class JsonBacktestVerdictStore(string directoryPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string DirectoryPath { get; } = directoryPath;

    public async Task SaveAsync(BacktestVerdict verdict, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DirectoryPath);
        var file = Path.Combine(DirectoryPath, $"{verdict.Symbol}-{verdict.TimeFrame}-{verdict.StrategyKind}.json");
        var json = JsonSerializer.Serialize(verdict, JsonOptions);
        await File.WriteAllTextAsync(file, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BacktestVerdict?> LoadLatestAsync(
        string symbol,
        string timeFrame,
        string strategyKind,
        CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(DirectoryPath, $"{symbol}-{timeFrame}-{strategyKind}.json");
        if (!File.Exists(file))
            return null;
        var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<BacktestVerdict>(json);
    }
}
