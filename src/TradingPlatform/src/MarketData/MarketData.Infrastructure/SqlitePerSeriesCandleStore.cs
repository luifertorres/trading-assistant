using MarketData.Application;
using MarketData.Domain;
using Microsoft.Data.Sqlite;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

/// <summary>One SQLite table per series (e.g. BTCUSDT_1min). Table names are not exposed outside MarketData.</summary>
public sealed class SqlitePerSeriesCandleStore : ICandleSeriesReader, ICandleSeriesWriter, IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public SqlitePerSeriesCandleStore(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
    }

    public async Task UpsertAsync(SeriesDescriptor series, IReadOnlyList<OhlcBar> bars, CancellationToken cancellationToken = default)
    {
        if (bars.Count == 0)
            return;
        var table = SeriesTableNaming.ToPhysicalTableName(series);
        await EnsureTableAsync(table, cancellationToken).ConfigureAwait(false);
        using var tx = _connection.BeginTransaction();
        var sql = $"""
            INSERT INTO "{table}" (OpenTime, CloseTime, Open, High, Low, Close, Volume)
            VALUES ($ot, $ct, $o, $h, $l, $c, $v)
            ON CONFLICT(OpenTime) DO UPDATE SET
              CloseTime = excluded.CloseTime,
              Open = excluded.Open, High = excluded.High, Low = excluded.Low,
              Close = excluded.Close, Volume = excluded.Volume
            """;
        foreach (var b in bars)
        {
            await using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$ot", b.OpenTime.ToUnixTimeMilliseconds());
            cmd.Parameters.AddWithValue("$ct", b.CloseTime.ToUnixTimeMilliseconds());
            cmd.Parameters.AddWithValue("$o", b.Open);
            cmd.Parameters.AddWithValue("$h", b.High);
            cmd.Parameters.AddWithValue("$l", b.Low);
            cmd.Parameters.AddWithValue("$c", b.Close);
            cmd.Parameters.AddWithValue("$v", b.Volume);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<OhlcBar>> ReadAsync(
        SeriesDescriptor series,
        DateTimeOffset? fromOpenTime,
        DateTimeOffset? toOpenTime,
        CancellationToken cancellationToken = default)
    {
        var table = SeriesTableNaming.ToPhysicalTableName(series);
        await EnsureTableAsync(table, cancellationToken).ConfigureAwait(false);
        var sql = $"""SELECT OpenTime, CloseTime, Open, High, Low, Close, Volume FROM "{table}" WHERE 1=1 """;
        if (fromOpenTime is { } f)
            sql += " AND OpenTime >= $from ";
        if (toOpenTime is { } t)
            sql += " AND OpenTime <= $to ";
        sql += " ORDER BY OpenTime ASC";
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        if (fromOpenTime is { } f2)
            cmd.Parameters.AddWithValue("$from", f2.ToUnixTimeMilliseconds());
        if (toOpenTime is { } t2)
            cmd.Parameters.AddWithValue("$to", t2.ToUnixTimeMilliseconds());
        var bars = new List<OhlcBar>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            bars.Add(new OhlcBar(
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(0)),
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6)));
        }

        return bars;
    }

    private async Task EnsureTableAsync(string table, CancellationToken ct)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS "{table}" (
              OpenTime INTEGER NOT NULL PRIMARY KEY,
              CloseTime INTEGER NOT NULL,
              Open REAL NOT NULL,
              High REAL NOT NULL,
              Low REAL NOT NULL,
              Close REAL NOT NULL,
              Volume REAL NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
