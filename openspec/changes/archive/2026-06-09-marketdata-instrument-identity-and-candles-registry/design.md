## Context

TradingPlatform **MarketData** today treats the raw Binance `symbol` string as domain identity (`SeriesDescriptor.Symbol`), physical SQLite table name (`SeriesTableNaming`), and the only persisted instrument metadata. `SqlitePerSeriesCandleStore` creates one quoted table per series; `Usdm1dBackfillOrchestrator` builds `SeriesDescriptor(symbol, TimeFrameCode.Day1)` and checkpoints by symbol string (v1 JSON).

That coupling caused Unicode and digit-prefixed contracts to fail at the storage-naming boundary (ADR-007 was a stopgap). It also prevents distinguishing `BTCUSDT` across venues/markets and discards useful `exchangeInfo` fields.

Storage is **greenfield**: `.trading-platform-data/market.sqlite` is gitignored and reproducible from Binance. This change replaces per-series tables with an **instrument registry** plus a **canonical `candles` table**, shifts kernel identity to opaque `InstrumentId`, and rewires the USD-M 1d backfill orchestration and checkpoint to use registry-resolved IDs. CLI flags and operator workflow stay the same; operators delete the old DB and checkpoint and re-run.

## Goals / Non-Goals

**Goals:**

- Introduce kernel `InstrumentId` and change `SeriesDescriptor` to `(InstrumentId, TimeFrameCode)`.
- Persist instrument metadata in an `instruments` table with unique key `(venue, market, contract_type, exchange_symbol)` and stable auto-allocated IDs.
- Replace `SqlitePerSeriesCandleStore` with `SqliteCandleStore`: single `candles` table keyed by `(instrument_id, timeframe_id, open_time_ms)`, plus seeded `timeframes` reference rows.
- Rewire `Usdm1dBackfillOrchestrator` to: refresh universe into registry → iterate `InstrumentId`s → write via `ICandleSeriesWriter`; checkpoint schema **v2** keyed by `InstrumentId` (v1 not loaded).
- Keep Binance.Net strictly in Infrastructure; Application ports remain broker-agnostic.
- Update `TradingVectorSpec` to carry `InstrumentId` and project `SeriesDescriptor` from it.
- Preserve existing backfill semantics: universe filter, forward paging, ascending batch writes, per-symbol failure isolation, Binance.Net rate limits, optional `exchangeInfo` snapshot (now including resolved `InstrumentId`s).

**Non-Goals:**

- Migrating data from legacy per-series tables (operators delete and re-backfill).
- Loading v1 checkpoint files (treat as fresh run).
- WebSocket live ingest, additional intervals, or legacy `TradingAssistant` integration.
- Multi-broker abstraction beyond the registry key shape and broker-agnostic upsert DTO.
- Changing CLI verb names or adding new operator flags (beyond what snapshot/checkpoint behavior already exposes).

## Decisions

### 1. `InstrumentId` is `long` (SQLite INTEGER)

**Chosen:** `public readonly record struct InstrumentId(long Value)` in `TradingPlatform.Kernel`, allocated by SQLite `AUTOINCREMENT` on insert into `instruments`.

**Rationale:** Compact primary/foreign keys, natural fit for single-file SQLite, simple JSON checkpoint serialization, no coordination needed for a solo-developer deployment.

**Alternatives:** `Guid` (better for distributed allocation without DB; heavier indexes and JSON); `string` slug derived from natural key (leaks exchange semantics into identity).

### 2. Single `market.sqlite` hosts registry and candles

**Chosen:** `instruments`, `timeframes`, and `candles` live in the same database file the CLI already passes as `--market-db`. One `SqliteConnection` opened per host lifetime (same pattern as today's `SqlitePerSeriesCandleStore`).

**Rationale:** Co-located data, one backup file, no cross-DB consistency issues.

**Alternatives:** Separate registry DB (more operator complexity); shared connection pool (overkill for CLI/backfill).

### 3. Registry schema and upsert semantics

**`instruments` table (conceptual):**

| Column | Type | Notes |
|--------|------|-------|
| `instrument_id` | INTEGER PK AUTOINCREMENT | Maps to `InstrumentId` |
| `venue` | TEXT NOT NULL | e.g. `binance` |
| `market` | TEXT NOT NULL | e.g. `usdm` |
| `contract_type` | TEXT NOT NULL | e.g. `perpetual` |
| `exchange_symbol` | TEXT NOT NULL | Raw Binance `symbol` |
| `base_asset`, `quote_asset`, `pair` | TEXT | From exchangeInfo |
| `price_precision`, `quantity_precision` | INTEGER | From exchangeInfo |
| `filters_json` | TEXT | Opaque JSON blob of filter array |
| `first_seen_utc`, `last_seen_utc` | TEXT (ISO-8601) | Set on insert / update |
| `last_status` | TEXT | e.g. `TRADING` |

**Unique constraint:** `(venue, market, contract_type, exchange_symbol)`.

**Upsert:** `INSERT … ON CONFLICT(venue, market, contract_type, exchange_symbol) DO UPDATE SET …` returning the stable `instrument_id`. `first_seen_utc` preserved on conflict; `last_seen_utc` and metadata fields updated.

**Application port:** `IInstrumentRegistry` with broker-agnostic `InstrumentUpsert` record (no Binance types). Domain entity `Instrument` in `MarketData.Domain` mirrors persisted fields for reads.

### 4. Canonical candles schema

**`timeframes` table:** seeded on first open with rows for each known `TimeFrameCode` (`code` TEXT UNIQUE, `timeframe_id` INTEGER PK). MVP seeds at least `Day1` / `1D`; additional codes added as contexts need them.

**`candles` table:**

| Column | Type |
|--------|------|
| `instrument_id` | INTEGER NOT NULL FK → `instruments` |
| `timeframe_id` | INTEGER NOT NULL FK → `timeframes` |
| `open_time_ms` | INTEGER NOT NULL |
| `close_time_ms`, OHLCV columns | Same types as today |
| **PK** | `(instrument_id, timeframe_id, open_time_ms)` |

**Indexes:** PK covers point lookups; add `(instrument_id, timeframe_id, open_time_ms)` covering index if needed (PK already suffices for range scans by open time).

**Write path:** `SqliteCandleStore` resolves `SeriesDescriptor.Instrument` + `TimeFrameCode` → `(instrument_id, timeframe_id)` via in-memory timeframe cache + direct ID use; all SQL uses bound parameters only.

**Read path:** Same resolution; `ORDER BY open_time_ms ASC`.

**Upsert:** `INSERT … ON CONFLICT(instrument_id, timeframe_id, open_time_ms) DO UPDATE SET …` (same semantics as per-series store).

### 5. Retire `SeriesTableNaming` and per-series store

**Chosen:** Delete `SeriesTableNaming.cs`, `SqlitePerSeriesCandleStore.cs`, and `SeriesTableNamingTests.cs`. `AddMarketDataSqlite` registers `SqliteInstrumentRegistry` + `SqliteCandleStore` (or a thin façade implementing both reader/writer ports).

**Rationale:** Spec mandates greenfield replacement; ADR-002 is superseded by this change (update ADR-002 text or add ADR-008 during implementation).

### 6. Kernel type changes

**`SeriesDescriptor`:** `(InstrumentId Instrument, TimeFrameCode TimeFrame)`. `Validate()` checks `Instrument.Value > 0` and non-empty timeframe.

**`TradingVectorSpec`:** Replace `string Symbol` with `InstrumentId Instrument`. `Series` property becomes `new SeriesDescriptor(Instrument, TimeFrame)`.

**Downstream:** Research (`BarProcessingContext`, `IBacktestRunner`, strategy factory), Execution (`PortfolioExecutionRouter`), and CLI demo code compile against the new shape. Display/logging resolves `exchange_symbol` via `IInstrumentRegistry.GetByIdAsync` when needed.

### 7. Backfill exchange port reshape

**Replace** `GetActiveUsdtPerpetualSymbolsAsync` with:

```csharp
Task<IReadOnlyList<UsdMInstrumentListing>> ListUsdtPerpetualInstrumentsAsync(CancellationToken ct);
```

`UsdMInstrumentListing` (Application DTO) carries:
- `InstrumentUpsert` — broker-agnostic fields for registry upsert
- `BrokerFetchHandle` — opaque struct; only Infrastructure unwraps it to the Binance symbol string

**Replace** kline method signature:

```csharp
Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
    BrokerFetchHandle handle,
    DateTimeOffset startTimeInclusive,
    DateTimeOffset endTimeInclusive,
    CancellationToken ct);
```

**Infrastructure:** `BinanceUsdM1dBackfillExchange` maps `BinanceFuturesUsdtSymbol` → `InstrumentUpsert` + `BrokerFetchHandle(symbol)` at listing time. Kline calls unwrap the handle internally. Filters serialized to `filters_json` with `System.Text.Json`.

**Venue constants** for USD-M backfill: `venue=binance`, `market=usdm`, `contract_type=perpetual` (string literals in Infrastructure mapper, not magic in orchestrator).

### 8. Orchestrator flow

```
Load checkpoint (v2 only; reject/null v1)
→ optional WriteExchangeInfoSnapshotAsync (extended)
→ listings = ListUsdtPerpetualInstrumentsAsync()
→ for each listing: registry.UpsertAsync → InstrumentId
→ sync checkpoint.Instruments[] by InstrumentId (drop stale IDs)
→ for each instrument:
      skip if entry.Complete
      series = SeriesDescriptor(instrumentId, Day1)
      page forward from LastWrittenOpenTimeMs + 1 day
      UpsertAsync(series, bars)
      update checkpoint after each batch
      catch/log/continue on failure (log exchange_symbol from registry for humans)
```

Remove all `SeriesTableNaming` usage and symbol-string checkpoint keys.

### 9. Checkpoint schema v2

**File:** same sidecar path as today (e.g. `.trading-platform-data/backfill-1d-checkpoint.json`).

**Shape:**

```json
{
  "schemaVersion": 2,
  "runId": "<guid>",
  "updatedAtUtc": "<iso>",
  "marketDatabasePath": "<full path>",
  "instruments": [
    {
      "instrumentId": 42,
      "complete": false,
      "lastWrittenOpenTimeMs": 1717200000000,
      "lastErrorMessage": null,
      "lastErrorAtUtc": null
    }
  ]
}
```

**Load rules:** If `schemaVersion != 2`, treat as **no checkpoint** (log once, start fresh). Do not attempt v1 → v2 migration.

**Resume:** Match `marketDatabasePath`; skip instruments with `complete: true`; continue from `lastWrittenOpenTimeMs + 1 day`.

### 10. ExchangeInfo snapshot extension

When `WriteExchangeInfoSnapshot` is enabled, write the raw Binance `exchangeInfo` JSON as today **and** a companion map (same file or sibling JSON) from `exchange_symbol` → `instrumentId` for every instrument upserted in that run. Exact file naming is an implementation detail; tasks should pick one documented path under `dataRoot`.

### 11. DI registration

`AddMarketDataSqlite(databasePath)`:
- Opens shared connection / database bootstrap (DDL for all three tables + timeframe seed)
- Registers `IInstrumentRegistry` → `SqliteInstrumentRegistry`
- Registers `ICandleSeriesReader` / `ICandleSeriesWriter` → `SqliteCandleStore`

`AddMarketDataBinanceUsdM1dBackfill` unchanged in surface; orchestrator constructor gains `IInstrumentRegistry`.

### 12. Testing strategy

- **Domain/Kernel:** `InstrumentId` equality; `SeriesDescriptor.Validate()` with invalid IDs.
- **Infrastructure:** SQLite registry upsert idempotency; candles upsert conflict; timeframe resolution; no SQL identifier interpolation (code review + integration tests).
- **Application:** Orchestrator tests with fake registry + exchange returning listings/handles; checkpoint v2 load/skip/continue; v1 file ignored.
- **Remove:** `SeriesTableNamingTests`.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| **Breaking change for Research/CLI demo** | Compile-fix all `SeriesDescriptor` / `TradingVectorSpec` call sites in same PR; tasks enumerate grep hits. |
| **Full re-download after deploy** | Document operator steps: delete `market.sqlite` + v1 checkpoint; expect long first run. |
| **v1 checkpoint silently discarded** | Log warning when old file detected; mention in CLI help / README. |
| **Single SQLite write throughput** | Sequential symbol loop unchanged; batch upserts in transactions as today. |
| **Handle indirection for klines** | Thin opaque struct; unwrap only in `BinanceUsdM1dBackfillExchange`. |
| **Timeframe seed drift** | Seed idempotently on DB open; add rows when new `TimeFrameCode` values ship. |
| **ADR-002 stale** | Supersede in `ADRs.md` when implementation lands. |

## Migration Plan

1. **Pre-deploy (operator):** Stop any running backfill. Delete `.trading-platform-data/market.sqlite` and `backfill-1d-checkpoint.json` (or use fresh `--market-db` / `--data-root`).
2. **Deploy:** Ship kernel + MarketData changes atomically (no partial upgrade — old code cannot read new DB).
3. **Post-deploy:** Run existing CLI backfill verb; registry populates on first exchangeInfo fetch; candles fill canonical table; v2 checkpoint written.
4. **Rollback:** Restore previous git tag/binaries **and** restore backed-up SQLite + v1 checkpoint if reverting code; new DB format is not readable by old binaries.

## Open Questions

- **ADR numbering:** Add ADR-008 for canonical candles table vs revise ADR-002 in place — decide during implementation PR.
- **Snapshot companion format:** Single augmented JSON vs sidecar `instrument-ids-{runId}.json` — either satisfies spec; pick one in tasks for documentation consistency.
- **Research backtests without registry:** Demo/CLI may hard-code known `InstrumentId`s after seeding, or add a small "resolve symbol → id" CLI helper — defer to tasks if not needed for MVP compile path.
