## Purpose

TradingPlatform MarketData SHALL support a **bulk backfill** of Binance USD-M Futures **1d** (daily) OHLCV history for all **actively trading USDT perpetual** symbols, persisted through the existing candle series writer into SQLite per-series storage.

## ADDED Requirements

### Requirement: Active USDT perpetual symbol universe

The backfill SHALL derive the symbol list from Binance USD-M Futures **exchange information** (or equivalent REST) and SHALL include only contracts that are **actively tradable** for this scope: `**TRADING`**, `**PERPETUAL**`, and `**USDT**` quote asset, unless a run-time configuration explicitly documents a different filter set.

#### Scenario: Symbol excluded when not tradable perpetual USDT

- **WHEN** exchange metadata shows a contract that is not `TRADING`, or not `PERPETUAL`, or does not use `USDT` as quote for the chosen scope
- **THEN** that symbol MUST NOT be included in the backfill universe for the run

#### Scenario: Universe is enumerable before download

- **WHEN** the backfill job starts
- **THEN** the implementation MUST obtain a finite list of symbols to process for that run before beginning per-symbol kline downloads

### Requirement: Full 1d kline history per symbol

For every symbol in the universe, the system SHALL fetch **all available daily (`1d`) klines** supported by the exchange for that symbol, using **paged REST requests** until no further history is returned, and SHALL map each candle into `OhlcBar` with correct open/close times and prices/volume.

#### Scenario: Pagination reaches listing inception

- **WHEN** a symbol has more daily candles than a single API page allows
- **THEN** the implementation MUST issue additional requests until the oldest available bar is retrieved or the API returns no further data for the requested window

#### Scenario: Daily series identity

- **WHEN** persisting bars for a symbol
- **THEN** the `SeriesDescriptor` MUST use the platform daily timeframe (`TimeFrameCode.Day1` / `1D`) consistent with kernel conventions, not an ad-hoc string that bypasses `SeriesTableNaming`

### Requirement: Chronological batch download and persist order

Binance USD-M Futures kline REST (and **Binance.Net**) support paging with `**startTime`** and `**endTime**`, so the implementation SHALL **compute each request window in chronological order** (oldest window first, advancing toward the present) and SHALL fetch batches that way whenever using that API. For each symbol, the backfill SHALL persist daily bars in **ascending `OpenTime` order at the batch level**: each `ICandleSeriesWriter.UpsertAsync` call for that symbol in a run MUST follow an **older** batch with a **newer** batch (inception → present). If an implementation instead uses reverse-time paging, it MUST buffer, sort, or otherwise reorder so that **writes** still satisfy the same ascending batch order.

#### Scenario: Forward windows use startTime and endTime

- **WHEN** requesting the next page of daily klines for a symbol after an earlier page has been retrieved
- **THEN** the implementation MUST use `startTime` / `endTime` (or Binance.Net parameters with the same meaning) so the next HTTP request asks for the **next chronological** slice (no request for a slice whose bars are entirely before the previous slice’s maximum `OpenTime`, unless retrying the same slice after a transient failure)

#### Scenario: Oldest batch written before newer batch

- **WHEN** two consecutive successful `UpsertAsync` calls target the same symbol in one uninterrupted backfill run
- **THEN** the maximum `OpenTime` among bars in the first call MUST be **strictly less than** the minimum `OpenTime` among bars in the second call (batches advance forward in time, oldest batch first)

#### Scenario: Reverse-time paging is reordered before write

- **WHEN** the implementation uses a paging strategy that returns klines in reverse chronological order per request
- **THEN** it MUST reorder or accumulate results so that `UpsertAsync` is still called in ascending `OpenTime` order batch-by-batch as required above

### Requirement: Persistence through MarketData writer

All ingested bars SHALL be written only through `**ICandleSeriesWriter.UpsertAsync`** (or a successor port with the same contract), so storage remains the existing **per-series SQLite** implementation and idempotent upsert behavior for a given `OpenTime` is preserved.

#### Scenario: Re-run does not duplicate logical bars

- **WHEN** the same daily bar (same symbol, same `OpenTime`) is ingested more than once
- **THEN** the store MUST retain a single row per `OpenTime` for that series (upsert semantics)

### Requirement: Broker isolation

Binance client types, REST DTOs, and **Binance.Net** (or equivalent) SHALL reside only in **Infrastructure** (or delivery wiring), MUST NOT appear in Domain or Application **public** contracts, and MUST map into kernel `**OhlcBar`** / `**SeriesDescriptor**` at the boundary.

#### Scenario: Application orchestration stays broker-agnostic

- **WHEN** Application-layer code orchestrates the backfill
- **THEN** it MUST depend on abstractions or use-case types that do not reference Binance-specific types

### Requirement: Delivery entry and operability

The product SHALL expose a **documented operator entry point** (e.g. CLI subcommand or explicit host one-shot) to start the backfill with configurable **database path** (or equivalent), logging of per-symbol progress, and SHALL document that the job is long-running and not part of normal application startup.

#### Scenario: Operator can run backfill explicitly

- **WHEN** an operator invokes the documented entry point
- **THEN** the backfill MUST run to completion or fail with logged errors without requiring the trading host’s steady-state loop

### Requirement: Rate limiting and resumability

Exchange **rate limits and retry/backoff** SHALL be satisfied **via Binance.Net** (aligned with [Binance USD-M futures general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)); the implementation SHALL **not** add a separate operator-configurable inter-request delay or duplicate backoff policy in application code. The implementation SHALL support **resuming** after interruption without re-downloading symbols (or time windows) already successfully checkpointed for that run. Long-running HTTP calls SHALL use a client configuration that avoids a restrictive default per-request timeout (e.g. infinite `HttpClient` timeout) so backfill is not aborted prematurely.

#### Scenario: Restart skips completed symbol

- **WHEN** the process stops after a symbol is fully ingested and checkpointed, and the job is started again with the same checkpoint state
- **THEN** that symbol MUST NOT be fully re-fetched from scratch unless the operator clears the checkpoint or requests a full refresh

### Requirement: Optional universe snapshot

The implementation MAY persist an optional **snapshot** of the symbol universe (and filter metadata) used for a run, so the exact set of symbols processed is auditable and reproducible for research.

#### Scenario: Snapshot written when enabled

- **WHEN** the snapshot option is enabled for a run
- **THEN** the system MUST write the snapshot artifact before or after the run in a documented location alongside or inside the configured data root