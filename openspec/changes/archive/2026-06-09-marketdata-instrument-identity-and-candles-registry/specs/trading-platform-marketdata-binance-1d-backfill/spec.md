## ADDED Requirements

### Requirement: Universe refresh into instrument registry

Before downloading klines, the backfill SHALL upsert every instrument in the active USDT perpetual universe into the instrument registry and SHALL iterate resolved `InstrumentId` values (not raw exchange symbol strings) for per-instrument download and persistence.

#### Scenario: Registry upsert before kline download

- **WHEN** the backfill job starts
- **THEN** the implementation MUST upsert all qualifying instruments from exchange metadata into the registry with idempotent `(venue, market, contract_type, exchange_symbol)` keys before issuing the first kline request for that run

#### Scenario: Iteration uses InstrumentId

- **WHEN** the orchestrator processes the universe for kline download and persistence
- **THEN** it MUST use stable `InstrumentId` values from the registry as the series identity, not ad-hoc exchange symbol strings in Application-layer orchestration

## MODIFIED Requirements

### Requirement: Full exchange symbol alphabet

The backfill SHALL accept every symbol returned by Binance USD-M `exchangeInfo` that already satisfies the universe filter (`status = TRADING`, `contractType = PERPETUAL`, `quoteAsset = USDT`), regardless of the symbol's character set. The implementation MUST NOT reject an otherwise-qualifying symbol on character-class grounds alone (for example, because it contains non-ASCII letters, Unicode code points outside `[A-Za-z0-9]`, or a leading digit). Character-set acceptance is enforced at universe selection and registry upsert; storage no longer derives SQL identifiers from symbol strings.

#### Scenario: Unicode symbols are accepted

- **WHEN** Binance USD-M `exchangeInfo` lists a `TRADING` `PERPETUAL` `USDT` contract whose `symbol` contains non-ASCII characters (for example `龙虾USDT`, `币安人生USDT`, `我踏马来了USDT`)
- **THEN** the backfill MUST include that contract in the run universe, upsert it into the instrument registry, resolve a stable `InstrumentId`, derive a `SeriesDescriptor(InstrumentId, TimeFrameCode.Day1)` for it, and persist its daily bars through `ICandleSeriesWriter.UpsertAsync` without rejecting the symbol on character-class grounds

#### Scenario: Digit-prefixed symbols are accepted

- **WHEN** Binance USD-M `exchangeInfo` lists a `TRADING` `PERPETUAL` `USDT` contract whose `symbol` starts with one or more ASCII digits (for example `1INCHUSDT`, `1000PEPEUSDT`, `1000000MOGUSDT`, `4USDT`)
- **THEN** the backfill MUST include that contract in the run universe, upsert it into the instrument registry, resolve a stable `InstrumentId`, derive a `SeriesDescriptor(InstrumentId, TimeFrameCode.Day1)` for it, and persist its daily bars through `ICandleSeriesWriter.UpsertAsync` without depending on any character-class rule that would disallow digit-prefixed names

### Requirement: Full 1d kline history per symbol

For every symbol in the universe, the system SHALL fetch **all available daily (`1d`) klines** supported by the exchange for that symbol, using **paged REST requests** until no further history is returned, and SHALL map each candle into `OhlcBar` with correct open/close times and prices/volume.

#### Scenario: Pagination reaches listing inception

- **WHEN** a symbol has more daily candles than a single API page allows
- **THEN** the implementation MUST issue additional requests until the oldest available bar is retrieved or the API returns no further data for the requested window

#### Scenario: Daily series identity

- **WHEN** persisting bars for an instrument
- **THEN** the `SeriesDescriptor` MUST use the instrument's resolved `InstrumentId` and the platform daily timeframe (`TimeFrameCode.Day1` / `1D`) consistent with kernel conventions, not an ad-hoc exchange symbol string that bypasses the instrument registry

### Requirement: Persistence through MarketData writer

All ingested bars SHALL be written only through `ICandleSeriesWriter.UpsertAsync` (or a successor port with the same contract), targeting the canonical **single-table candles store** (`trading-platform-marketdata-candles-store`). Storage MUST NOT use per-series physical table names derived from exchange symbols. Idempotent upsert behavior for a given `OpenTime` within a series MUST be preserved.

#### Scenario: Re-run does not duplicate logical bars

- **WHEN** the same daily bar (same instrument, same timeframe, same `OpenTime`) is ingested more than once
- **THEN** the store MUST retain a single row per `(instrument_id, timeframe_id, open_time_ms)` (upsert semantics)

### Requirement: Per-symbol failure isolation

A single symbol's failure SHALL NOT abort the remainder of a backfill run. When the orchestrator processes the universe in a single run, any exception thrown while handling one symbol (including failures in instrument registry resolution, checkpoint update, exchange calls, or the candle writer) MUST be caught at symbol scope, logged with the offending symbol and a human-readable error, and recorded on that symbol's checkpoint entry so operators can inspect which symbols succeeded and which failed. The orchestrator MUST then continue with the next symbol in the universe and MUST return normally to its caller if all remaining symbols complete (successfully or with their own recorded failures).

#### Scenario: One failing symbol does not abort the run

- **WHEN** a backfill run iterates the symbol universe and processing one symbol throws an exception
- **THEN** the orchestrator MUST log the failure, record it on that symbol's checkpoint entry, and continue with the next symbol rather than propagating the exception out of the run

#### Scenario: Failure is observable on the checkpoint

- **WHEN** a symbol has failed at least once during the current run
- **THEN** the checkpoint entry for that symbol MUST carry enough information for an operator to identify the failure (at minimum: the error message and a UTC timestamp of the most recent failure), and the symbol MUST NOT be marked `complete` unless its backfill later succeeds

#### Scenario: Healthy symbols still complete normally

- **WHEN** a run contains a mix of failing and healthy symbols
- **THEN** every healthy symbol MUST be processed to completion and have its bars persisted, regardless of the position of failing symbols in the iteration order

### Requirement: Rate limiting and resumability

Exchange **rate limits and retry/backoff** SHALL be satisfied **via Binance.Net** (aligned with [Binance USD-M futures general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)); the implementation SHALL **not** add a separate operator-configurable inter-request delay or duplicate backoff policy in application code. The implementation SHALL support **resuming** after interruption without re-downloading symbols (or time windows) already successfully checkpointed for that run. Long-running HTTP calls SHALL use a client configuration that avoids a restrictive default per-request timeout (e.g. infinite `HttpClient` timeout) so backfill is not aborted prematurely. Checkpoint state MUST use schema version 2 keyed by `InstrumentId`; v1 checkpoint files MUST NOT be loaded.

#### Scenario: Checkpoint schema v2 keyed by InstrumentId

- **WHEN** the backfill persists checkpoint state for resumability
- **THEN** the checkpoint document MUST use schema version 2 with per-instrument entries keyed by `InstrumentId` (each entry retains `LastWrittenOpenTimeMs` and `Complete` as in v1)
- **AND** v1 checkpoint files MUST NOT be loaded (operators delete the old checkpoint or start fresh alongside the greenfield database)

#### Scenario: Restart skips completed symbol

- **WHEN** the process stops after a symbol is fully ingested and checkpointed, and the job is started again with the same checkpoint state
- **THEN** that symbol MUST NOT be fully re-fetched from scratch unless the operator clears the checkpoint or requests a full refresh

### Requirement: Optional universe snapshot

The implementation MAY persist an optional **snapshot** of the symbol universe (and filter metadata) used for a run, so the exact set of symbols processed is auditable and reproducible for research.

#### Scenario: Snapshot written when enabled

- **WHEN** the snapshot option is enabled for a run
- **THEN** the system MUST write the snapshot artifact before or after the run in a documented location alongside or inside the configured data root
- **AND** the snapshot MUST record the resolved `InstrumentId` for each instrument processed alongside the raw `exchangeInfo` rows used for that run

## REMOVED Requirements

### Requirement: Unsafe storage characters scenario (from Full exchange symbol alphabet)

**Reason**: Per-series SQL table naming is retired; the canonical candles store keys rows by `instrument_id`, so symbol-character validation at a SQL-identifier boundary is no longer applicable.

**Migration**: No operator action required beyond the greenfield database reset already mandated by this change. Unicode and digit-prefixed symbols are accepted via registry upsert without storage-layer character rejection.
