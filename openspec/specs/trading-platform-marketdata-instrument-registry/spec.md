## Purpose

TradingPlatform MarketData SHALL maintain a **persistent instrument registry** that assigns stable opaque `InstrumentId` values and stores exchange metadata keyed by `(venue, market, contract_type, exchange_symbol)`.

## Requirements

### Requirement: Stable opaque instrument identity

The MarketData context SHALL assign each tradable instrument a stable opaque `InstrumentId` (kernel value type) that is the canonical identity for candle series, checkpoints, and cross-context references. `InstrumentId` MUST NOT be derived from the exchange symbol string at read time; it MUST be allocated by the registry and remain stable across runs for the same unique registry key.

#### Scenario: Same exchange contract receives same InstrumentId on re-run

- **WHEN** the registry upserts an instrument with the same `(venue, market, contract_type, exchange_symbol)` as a prior run
- **THEN** the system MUST return the existing `InstrumentId` for that key and MUST NOT allocate a new identity

#### Scenario: InstrumentId is opaque to callers

- **WHEN** Application or downstream contexts reference a candle series
- **THEN** they MUST use `InstrumentId` in `SeriesDescriptor` rather than treating `exchange_symbol` as the series identity

### Requirement: Unique registry key

The instrument registry SHALL enforce uniqueness on `(venue, market, contract_type, exchange_symbol)`. Upsert operations MUST be idempotent on that key: repeated upserts update metadata and `last_seen_utc` without creating duplicate rows.

#### Scenario: Idempotent upsert on natural key

- **WHEN** `UpsertAsync` is called twice with the same venue, market, contract type, and exchange symbol
- **THEN** the registry MUST contain exactly one row for that key and both calls MUST resolve to the same `InstrumentId`

#### Scenario: Distinct markets do not collide

- **WHEN** two instruments share the same `exchange_symbol` but differ in `venue`, `market`, or `contract_type`
- **THEN** the registry MUST store them as separate rows with distinct `InstrumentId` values

### Requirement: Instrument metadata persistence

The registry SHALL persist, at minimum, per-instrument fields: `InstrumentId`, `venue`, `market`, `contract_type`, `exchange_symbol`, `base_asset`, `quote_asset`, `pair`, price precision, quantity precision, an opaque `filters_json` blob, `first_seen_utc`, `last_seen_utc`, and `last_status`. Exchange symbol and base/quote assets are data fields on the registry row, not domain identity.

#### Scenario: Metadata fields stored on upsert

- **WHEN** an instrument upsert is accepted with venue, market, contract type, exchange symbol, base/quote assets, pair, precisions, filters, and status from exchange metadata
- **THEN** the registry MUST persist all supplied fields on the row keyed by the unique registry key

#### Scenario: First and last seen timestamps maintained

- **WHEN** an instrument is upserted for the first time
- **THEN** `first_seen_utc` MUST be set to the upsert time
- **WHEN** the same instrument is upserted again on a later run
- **THEN** `last_seen_utc` MUST be updated to the latest upsert time while `first_seen_utc` remains unchanged

### Requirement: Registry application port

MarketData.Application SHALL expose `IInstrumentRegistry` with at least: `UpsertAsync` (accepting a broker-agnostic upsert DTO), `GetByIdAsync`, `GetByExchangeSymbolAsync(venue, market, contractType, exchangeSymbol)`, and `ListAllAsync`. Infrastructure MUST implement persistence (e.g. SQLite `instruments` table) without exposing storage details through the port.

#### Scenario: Lookup by exchange natural key

- **WHEN** a caller requests an instrument by venue, market, contract type, and exchange symbol after a successful upsert
- **THEN** `GetByExchangeSymbolAsync` MUST return the row with the stable `InstrumentId` and stored metadata

#### Scenario: List all instruments

- **WHEN** a caller invokes `ListAllAsync`
- **THEN** the registry MUST return all persisted instruments with their `InstrumentId` and metadata fields

### Requirement: Broker isolation at registry edge

Mapping from broker-specific exchange metadata (e.g. Binance USD-M `exchangeInfo` rows) into registry upsert inputs SHALL occur only in MarketData.Infrastructure. Application-layer registry contracts MUST NOT reference Binance.Net or other broker types.

#### Scenario: Application upsert DTO is broker-agnostic

- **WHEN** Application code calls `IInstrumentRegistry.UpsertAsync`
- **THEN** the upsert input MUST use broker-neutral field names (venue, market, contract type, exchange symbol, etc.) with no Binance-specific types in the public contract
