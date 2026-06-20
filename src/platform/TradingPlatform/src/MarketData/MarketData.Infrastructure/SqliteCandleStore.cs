using MarketData.Application;
using Microsoft.Data.Sqlite;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

public sealed class SqliteCandleStore(SqliteMarketDatabase database) : ICandleSeriesReader, ICandleSeriesWriter
{
    private readonly Dictionary<string, int> _timeframeIdByCode = new(StringComparer.Ordinal);

    public async Task UpsertAsync(SeriesDescriptor series, IReadOnlyList<OhlcBar> bars, CancellationToken cancellationToken = default)
    {
        if (bars.Count == 0)
            return;

        series.Validate();
        var timeframeId = await ResolveTimeframeIdAsync(series.TimeFrame, cancellationToken).ConfigureAwait(false);
        var instrumentId = series.Instrument.Value;

        using var tx = database.Connection.BeginTransaction();
        const string sql = """
            INSERT INTO candles (
              instrument_id, timeframe_id, open_time_ms, close_time_ms,
              open, high, low, close, volume)
            VALUES ($instrumentId, $timeframeId, $ot, $ct, $o, $h, $l, $c, $v)
            ON CONFLICT(instrument_id, timeframe_id, open_time_ms) DO UPDATE SET
              close_time_ms = excluded.close_time_ms,
              open = excluded.open, high = excluded.high, low = excluded.low,
              close = excluded.close, volume = excluded.volume
            """;

        foreach (var b in bars)
        {
            await using var cmd = database.Connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$instrumentId", instrumentId);
            cmd.Parameters.AddWithValue("$timeframeId", timeframeId);
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
        series.Validate();
        var timeframeId = await ResolveTimeframeIdAsync(series.TimeFrame, cancellationToken).ConfigureAwait(false);
        var instrumentId = series.Instrument.Value;

        var sql = """
            SELECT open_time_ms, close_time_ms, open, high, low, close, volume
            FROM candles
            WHERE instrument_id = $instrumentId AND timeframe_id = $timeframeId
            """;
        if (fromOpenTime is { } f)
            sql += " AND open_time_ms >= $from ";
        if (toOpenTime is { } t)
            sql += " AND open_time_ms <= $to ";
        sql += " ORDER BY open_time_ms ASC";

        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$instrumentId", instrumentId);
        cmd.Parameters.AddWithValue("$timeframeId", timeframeId);
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

    private async Task<int> ResolveTimeframeIdAsync(TimeFrameCode timeFrame, CancellationToken ct)
    {
        if (_timeframeIdByCode.TryGetValue(timeFrame.Value, out var cached))
            return cached;

        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = "SELECT timeframe_id FROM timeframes WHERE code = $code";
        cmd.Parameters.AddWithValue("$code", timeFrame.Value);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (result is null)
            throw new InvalidOperationException($"Unknown timeframe code '{timeFrame.Value}'.");

        var id = Convert.ToInt32(result);
        _timeframeIdByCode[timeFrame.Value] = id;
        return id;
    }
}
