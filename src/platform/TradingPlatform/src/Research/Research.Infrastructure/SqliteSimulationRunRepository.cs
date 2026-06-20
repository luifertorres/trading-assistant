using System.Text.Json;
using Microsoft.Data.Sqlite;
using Research.Application;
using Research.Domain;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

public sealed class SqliteSimulationRunRepository : ISimulationRunRepository, IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteSimulationRunRepository(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    public async Task SaveAsync(SimulationRunResult result, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        var dto = RunDto.From(result);
        var json = JsonSerializer.Serialize(dto);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SimulationRuns (RunId, VectorId, PayloadJson, FinalEquity, MaxDrawdownFraction)
            VALUES ($id, $vid, $json, $fe, $dd)
            """;
        cmd.Parameters.AddWithValue("$id", result.RunId.ToString());
        cmd.Parameters.AddWithValue("$vid", result.VectorId.Value.ToString());
        cmd.Parameters.AddWithValue("$json", json);
        cmd.Parameters.AddWithValue("$fe", result.FinalEquity);
        cmd.Parameters.AddWithValue("$dd", result.MaxDrawdownFraction);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SimulationRunResult>> ListRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT PayloadJson FROM SimulationRuns ORDER BY rowid DESC LIMIT $take
            """;
        cmd.Parameters.AddWithValue("$take", take);
        var results = new List<SimulationRunResult>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var dto = JsonSerializer.Deserialize<RunDto>(reader.GetString(0));
            if (dto is not null)
                results.Add(dto.ToResult());
        }

        return results;
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS SimulationRuns (
              RunId TEXT NOT NULL PRIMARY KEY,
              VectorId TEXT NOT NULL,
              PayloadJson TEXT NOT NULL,
              FinalEquity REAL NOT NULL,
              MaxDrawdownFraction REAL NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private sealed record RunDto(
        Guid RunId,
        Guid VectorId,
        SimulationConfiguration Configuration,
        List<TradeRecord> Trades,
        List<EquityPoint> Equity,
        decimal FinalEquity,
        decimal MaxDrawdownFraction)
    {
        public static RunDto From(SimulationRunResult r) =>
            new(
                r.RunId,
                r.VectorId.Value,
                r.Configuration,
                r.Trades.ToList(),
                r.Equity.ToList(),
                r.FinalEquity,
                r.MaxDrawdownFraction);

        public SimulationRunResult ToResult() =>
            new(
                TradingVectorId.From(VectorId),
                RunId,
                Configuration,
                Trades,
                Equity,
                FinalEquity,
                MaxDrawdownFraction);
    }
}
