## ADDED Requirements

### Requirement: Canonical single-table candle storage

MarketData SHALL persist OHLCV bars in a single `candles` table keyed by `(instrument_id, timeframe_id, open_time_ms)`. The store MUST NOT create per-series physical tables or derive SQL identifiers from exchange symbol strings.

#### Scenario: All series share one candles table

- **WHEN** bars for two different instruments are persisted
- **THEN** both MUST be stored in the same `candles` table distinguished by `instrument_id` and `timeframe_id`, not separate tables per symbol

#### Scenario: No symbol-derived SQL identifiers

- **WHEN** the candles store executes read or write SQL
- **THEN** it MUST NOT interpolate exchange symbol strings into table or column identifiers; all series discrimination MUST use bound parameters on `instrument_id` and `timeframe_id`

### Requirement: SeriesDescriptor read and write contract

`ICandleSeriesReader` and `ICandleSeriesWriter` SHALL accept `SeriesDescriptor` composed of `(InstrumentId, TimeFrameCode)` for all operations. Callers MUST NOT pass physical table names or raw exchange symbols to the store ports.

#### Scenario: Writer upsert by SeriesDescriptor

- **WHEN** `ICandleSeriesWriter.UpsertAsync` is called with `SeriesDescriptor(InstrumentId, TimeFrameCode)`
- **THEN** the store MUST resolve the instrument and timeframe to storage keys and upsert bars without requiring a symbol string

#### Scenario: Reader query by SeriesDescriptor

- **WHEN** `ICandleSeriesReader.ReadAsync` is called with a `SeriesDescriptor` and optional open-time range
- **THEN** the store MUST return matching `OhlcBar` rows for that instrument and timeframe only

### Requirement: Idempotent upsert on open time

For a given `(instrument_id, timeframe_id, open_time_ms)`, the store SHALL retain at most one logical bar. Re-ingesting a bar with the same open time MUST update the existing row (upsert semantics), matching prior per-series store behavior.

#### Scenario: Duplicate open time overwrites

- **WHEN** two upsert calls include a bar with the same `OpenTime` for the same `SeriesDescriptor`
- **THEN** the store MUST contain a single row for that open time with values from the latest upsert

### Requirement: Chronological read order

`ICandleSeriesReader.ReadAsync` SHALL return bars ordered by `OpenTime` ascending within the requested range.

#### Scenario: Ascending open times in result

- **WHEN** a read returns multiple bars for a series
- **THEN** each bar's `OpenTime` MUST be greater than or equal to the previous bar's `OpenTime`

### Requirement: Timeframe reference table

The store SHALL maintain a `timeframes` reference table (or equivalent) mapping `TimeFrameCode` values to stable `timeframe_id` keys used in the `candles` table foreign key.

#### Scenario: Day1 timeframe resolves to stable id

- **WHEN** bars are written for `TimeFrameCode.Day1`
- **THEN** the store MUST persist them with the same `timeframe_id` on every run for that timeframe code

### Requirement: Greenfield storage replacement

The per-series SQLite layout (`SqlitePerSeriesCandleStore`, `SeriesTableNaming`) is retired. Operators SHALL delete the existing `market.sqlite` file (or point the CLI at a fresh database path) and re-run ingestion; no migration from per-series tables is provided.

#### Scenario: Fresh database on first use

- **WHEN** the candles store opens a database that has no `candles` table
- **THEN** it MUST create the canonical schema (`candles`, `timeframes`, and required indexes) without creating legacy per-symbol tables
