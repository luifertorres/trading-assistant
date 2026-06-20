using MarketData.Application;
using MarketData.Domain;
using Microsoft.Data.Sqlite;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

public sealed class SqliteInstrumentRegistry(SqliteMarketDatabase database) : IInstrumentRegistry
{
    public async Task<InstrumentId> UpsertAsync(InstrumentUpsert upsert, CancellationToken cancellationToken = default)
    {
        var seen = upsert.SeenAtUtc.ToString("O");
        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO instruments (
              venue, market, contract_type, exchange_symbol,
              base_asset, quote_asset, pair,
              price_precision, quantity_precision, filters_json,
              first_seen_utc, last_seen_utc, last_status)
            VALUES (
              $venue, $market, $contractType, $exchangeSymbol,
              $baseAsset, $quoteAsset, $pair,
              $pricePrecision, $quantityPrecision, $filtersJson,
              $seen, $seen, $lastStatus)
            ON CONFLICT(venue, market, contract_type, exchange_symbol) DO UPDATE SET
              base_asset = excluded.base_asset,
              quote_asset = excluded.quote_asset,
              pair = excluded.pair,
              price_precision = excluded.price_precision,
              quantity_precision = excluded.quantity_precision,
              filters_json = excluded.filters_json,
              last_seen_utc = excluded.last_seen_utc,
              last_status = excluded.last_status
            RETURNING instrument_id
            """;
        cmd.Parameters.AddWithValue("$venue", upsert.Venue);
        cmd.Parameters.AddWithValue("$market", upsert.Market);
        cmd.Parameters.AddWithValue("$contractType", upsert.ContractType);
        cmd.Parameters.AddWithValue("$exchangeSymbol", upsert.ExchangeSymbol);
        cmd.Parameters.AddWithValue("$baseAsset", upsert.BaseAsset);
        cmd.Parameters.AddWithValue("$quoteAsset", upsert.QuoteAsset);
        cmd.Parameters.AddWithValue("$pair", upsert.Pair);
        cmd.Parameters.AddWithValue("$pricePrecision", upsert.PricePrecision);
        cmd.Parameters.AddWithValue("$quantityPrecision", upsert.QuantityPrecision);
        cmd.Parameters.AddWithValue("$filtersJson", upsert.FiltersJson);
        cmd.Parameters.AddWithValue("$seen", seen);
        cmd.Parameters.AddWithValue("$lastStatus", upsert.LastStatus);

        var id = (long)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        return new InstrumentId(id);
    }

    public async Task<Instrument?> GetByIdAsync(InstrumentId id, CancellationToken cancellationToken = default)
    {
        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = SelectInstrumentSql + " WHERE instrument_id = $id";
        cmd.Parameters.AddWithValue("$id", id.Value);
        return await ReadSingleAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Instrument?> GetByExchangeSymbolAsync(
        string venue,
        string market,
        string contractType,
        string exchangeSymbol,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = SelectInstrumentSql + """
             WHERE venue = $venue AND market = $market
               AND contract_type = $contractType AND exchange_symbol = $exchangeSymbol
            """;
        cmd.Parameters.AddWithValue("$venue", venue);
        cmd.Parameters.AddWithValue("$market", market);
        cmd.Parameters.AddWithValue("$contractType", contractType);
        cmd.Parameters.AddWithValue("$exchangeSymbol", exchangeSymbol);
        return await ReadSingleAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Instrument>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = database.Connection.CreateCommand();
        cmd.CommandText = SelectInstrumentSql + " ORDER BY exchange_symbol ASC";
        var list = new List<Instrument>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(ReadInstrument(reader));
        return list;
    }

    private const string SelectInstrumentSql = """
        SELECT instrument_id, venue, market, contract_type, exchange_symbol,
               base_asset, quote_asset, pair, price_precision, quantity_precision,
               filters_json, first_seen_utc, last_seen_utc, last_status
        FROM instruments
        """;

    private static async Task<Instrument?> ReadSingleAsync(SqliteCommand cmd, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;
        return ReadInstrument(reader);
    }

    private static Instrument ReadInstrument(SqliteDataReader reader) =>
        new()
        {
            Id = new InstrumentId(reader.GetInt64(0)),
            Venue = reader.GetString(1),
            Market = reader.GetString(2),
            ContractType = reader.GetString(3),
            ExchangeSymbol = reader.GetString(4),
            BaseAsset = reader.GetString(5),
            QuoteAsset = reader.GetString(6),
            Pair = reader.GetString(7),
            PricePrecision = reader.GetInt32(8),
            QuantityPrecision = reader.GetInt32(9),
            FiltersJson = reader.GetString(10),
            FirstSeenUtc = DateTimeOffset.Parse(reader.GetString(11)),
            LastSeenUtc = DateTimeOffset.Parse(reader.GetString(12)),
            LastStatus = reader.GetString(13)
        };
}
