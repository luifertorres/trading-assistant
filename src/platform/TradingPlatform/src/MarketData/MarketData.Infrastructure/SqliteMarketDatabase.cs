using Microsoft.Data.Sqlite;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

/// <summary>Shared SQLite connection with registry and candles DDL.</summary>
public sealed class SqliteMarketDatabase : IAsyncDisposable, IDisposable
{
    private static readonly TimeFrameCode[] SeedTimeFrames =
    [
        TimeFrameCode.Sec1,
        TimeFrameCode.Min1,
        TimeFrameCode.Min3,
        TimeFrameCode.Min5,
        TimeFrameCode.Min15,
        TimeFrameCode.Min30,
        TimeFrameCode.Hour1,
        TimeFrameCode.Hour2,
        TimeFrameCode.Hour4,
        TimeFrameCode.Hour6,
        TimeFrameCode.Hour8,
        TimeFrameCode.Hour12,
        TimeFrameCode.Day1,
        TimeFrameCode.Week1,
        TimeFrameCode.Month1
    ];

    public SqliteConnection Connection { get; }

    public SqliteMarketDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        Connection = new SqliteConnection($"Data Source={databasePath}");
        Connection.Open();
        Bootstrap();
    }

    private void Bootstrap()
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS instruments (
              instrument_id INTEGER PRIMARY KEY AUTOINCREMENT,
              venue TEXT NOT NULL,
              market TEXT NOT NULL,
              contract_type TEXT NOT NULL,
              exchange_symbol TEXT NOT NULL,
              base_asset TEXT NOT NULL,
              quote_asset TEXT NOT NULL,
              pair TEXT NOT NULL,
              price_precision INTEGER NOT NULL,
              quantity_precision INTEGER NOT NULL,
              filters_json TEXT NOT NULL,
              first_seen_utc TEXT NOT NULL,
              last_seen_utc TEXT NOT NULL,
              last_status TEXT NOT NULL,
              UNIQUE (venue, market, contract_type, exchange_symbol)
            );

            CREATE TABLE IF NOT EXISTS timeframes (
              timeframe_id INTEGER PRIMARY KEY AUTOINCREMENT,
              code TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS candles (
              instrument_id INTEGER NOT NULL,
              timeframe_id INTEGER NOT NULL,
              open_time_ms INTEGER NOT NULL,
              close_time_ms INTEGER NOT NULL,
              open REAL NOT NULL,
              high REAL NOT NULL,
              low REAL NOT NULL,
              close REAL NOT NULL,
              volume REAL NOT NULL,
              PRIMARY KEY (instrument_id, timeframe_id, open_time_ms),
              FOREIGN KEY (instrument_id) REFERENCES instruments(instrument_id),
              FOREIGN KEY (timeframe_id) REFERENCES timeframes(timeframe_id)
            );
            """;
        cmd.ExecuteNonQuery();

        foreach (var tf in SeedTimeFrames)
        {
            using var seed = Connection.CreateCommand();
            seed.CommandText = "INSERT OR IGNORE INTO timeframes (code) VALUES ($code)";
            seed.Parameters.AddWithValue("$code", tf.Value);
            seed.ExecuteNonQuery();
        }
    }

    public void Dispose() => Connection.Dispose();

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
