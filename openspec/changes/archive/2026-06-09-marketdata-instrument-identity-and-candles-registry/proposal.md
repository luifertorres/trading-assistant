## Why

Today the USD-M backfill smears one concept across three layers: the raw exchange `symbol` string is simultaneously the domain identity (`SeriesDescriptor.Symbol`), the physical SQL table-name component (`SeriesTableNaming`), and the only thing we remember about an instrument. That is why a Unicode-named contract can crash the pipeline at the storage-naming step, why `BTCUSDT` on USD-M and a future spot/COIN-M `BTCUSDT` would silently collide, and why we throw away every other useful field Binance returns (`baseAsset`, `quoteAsset`, `contractType`, `pair`, precisions, filters). The short-term fix lands under `marketdata-usdm-backfill-unicode-symbol-fix`; this change fixes the identity and storage model underneath it. Storage is greenfield: `.trading-platform-data/market.sqlite` is already gitignored and reproducible from Binance, so we drop the per-series table layout entirely rather than migrate it.

## What Changes

- **BREAKING — storage**: retire the per-series table layout. The file at `.trading-platform-data/market.sqlite` (and any equivalent under a configured `DataRoot`) is treated as disposable; operators are expected to delete it and re-run the backfill. `SqlitePerSeriesCandleStore` and `SeriesTableNaming` are removed.
- **BREAKING — kernel identity**: add an opaque `InstrumentId` value type to `TradingPlatform.Kernel`. `SeriesDescriptor` moves from `(string Symbol, TimeFrameCode TimeFrame)` to `(InstrumentId Instrument, TimeFrameCode TimeFrame)`. `string Symbol` and `string BaseAsset` / `string QuoteAsset` remain data fields on the registry row, not domain identity. `TradingVectorSpec` (which today holds a `string Symbol`) is adjusted to carry `InstrumentId` and to project a `SeriesDescriptor` from it.
- **New capability — instrument registry**: a MarketData-owned registry persists per-instrument metadata with a unique key of `(venue, market, contract_type, exchange_symbol)`. Row fields include `InstrumentId`, `venue`, `market`, `contract_type`, `exchange_symbol`, `base_asset`, `quote_asset`, `pair`, price/quantity precisions, an opaque `filters_json`, `first_seen_utc`, `last_seen_utc`, `last_status`. Upsert is idempotent by unique key; `InstrumentId` is stable across runs.
- **New capability — canonical candles store**: a single `candles` table keyed by `(instrument_id, timeframe_id, open_time_ms)` with the existing OHLCV columns and idempotent upsert (same `OpenTime` wins as today). Readers and writers accept `SeriesDescriptor`, never a physical table name. No hand-built SQL identifiers anywhere.
- **Modified capability — USD-M 1d backfill**: orchestration becomes "refresh universe into the registry → iterate `InstrumentId`s → write via the new candles writer". `BackfillCheckpointDocument` goes to **schema v2**, keyed by `InstrumentId`; v1 files are no longer loaded (consistent with greenfield storage).
- **CLI behavior preserved**: the existing `TradingPlatform.Cli` backfill verb keeps its flags and semantics; only the underlying store and identity change. The optional `exchangeInfo` snapshot is retained and augmented with the resolved `InstrumentId`s used for that run.
- **Broker isolation preserved**: Binance.Net stays strictly in `MarketData.Infrastructure`. The ACL that turns a `BinanceFuturesUsdtSymbol` into a registry upsert and an `InstrumentId` lives at that edge.

## Capabilities

### New Capabilities

- `trading-platform-marketdata-instrument-registry`: persistent per-instrument metadata (venue, market, contract type, exchange symbol, base/quote, pair, precisions, filters, first/last-seen, last status) keyed by a stable opaque `InstrumentId`, with idempotent upsert on `(venue, market, contract_type, exchange_symbol)`.
- `trading-platform-marketdata-candles-store`: canonical single-table OHLCV storage keyed by `(instrument_id, timeframe_id, open_time_ms)` with idempotent upsert on same-`OpenTime` bars and reads ordered by `OpenTime` ascending.

### Modified Capabilities

- `trading-platform-marketdata-binance-1d-backfill`:
  - The "Persistence through MarketData writer" requirement is restated against the new candles store.
  - The "Daily series identity" scenario is rewritten in terms of `SeriesDescriptor(InstrumentId, TimeFrameCode.Day1)`; the rule forbidding ad-hoc strings that bypass `SeriesTableNaming` becomes "ad-hoc strings that bypass the registry".
  - The "Rate limiting and resumability" requirement gains a scenario defining the v2 checkpoint schema keyed by `InstrumentId` (v1 is not loaded).
  - The "Optional universe snapshot" scenario is extended: when enabled, the snapshot records the resolved `InstrumentId`s alongside the raw `exchangeInfo` rows for the run.

## Impact

- **Code — new:**
  - `src/TradingPlatform/src/BuildingBlocks/TradingPlatform.Kernel/InstrumentId.cs` — opaque wrapper (record struct over `long` or `Guid`; decision belongs in `design.md`).
  - `src/TradingPlatform/src/MarketData/MarketData.Domain/Instrument.cs` — domain entity with the registry fields listed above.
  - `src/TradingPlatform/src/MarketData/MarketData.Application/IInstrumentRegistry.cs` — `UpsertAsync(InstrumentUpsert)`, `GetByIdAsync`, `GetByExchangeSymbolAsync(venue, market, contractType, symbol)`, `ListAllAsync`.
  - `src/TradingPlatform/src/MarketData/MarketData.Infrastructure/SqliteInstrumentRegistry.cs` — `instruments` table + DDL.
  - `src/TradingPlatform/src/MarketData/MarketData.Infrastructure/SqliteCandleStore.cs` — `candles` + `timeframes` tables; implements `ICandleSeriesReader` and `ICandleSeriesWriter`.
- **Code — changed:**
  - `src/TradingPlatform/src/BuildingBlocks/TradingPlatform.Kernel/SeriesDescriptor.cs` — identity shifts to `InstrumentId`.
  - `src/TradingPlatform/src/BuildingBlocks/TradingPlatform.Kernel/TradingVectorSpec.cs` — `Symbol` → `InstrumentId`.
  - `src/TradingPlatform/src/MarketData/MarketData.Infrastructure/BinanceUsdM1dBackfillExchange.cs` — returns registry upsert inputs (venue/market/contractType/exchangeSymbol/base/quote/pair/precisions/filters/status) instead of bare `string`s.
  - `src/TradingPlatform/src/MarketData/MarketData.Application/IUsdM1dBackfillExchange.cs` — `GetActiveUsdtPerpetualSymbolsAsync` replaced by `ListUsdtPerpetualInstrumentsAsync` returning broker-agnostic DTOs for registry upsert; `GetDailyKlinesPageAsync` accepts a resolved broker-specific handle rather than a raw string, keeping exchange strings out of Application public types.
  - `src/TradingPlatform/src/MarketData/MarketData.Application/Usdm1dBackfillOrchestrator.cs` — universe upsert → instrument-id loop → writer; no `SeriesTableNaming` call.
  - `src/TradingPlatform/src/MarketData/MarketData.Application/BackfillCheckpointDocumentV2.cs` — keyed by `InstrumentId`, retains `LastWrittenOpenTimeMs` + `Complete`.
- **Code — removed:**
  - `src/TradingPlatform/src/MarketData/MarketData.Domain/SeriesTableNaming.cs`
  - `src/TradingPlatform/src/MarketData/MarketData.Infrastructure/SqlitePerSeriesCandleStore.cs`
  - Associated tests under `tests/MarketData.Domain.Tests/SeriesTableNamingTests.cs`.
- **Dependencies:** No new NuGet packages. Binance.Net stays at the same version pinned by the existing infrastructure project.
- **Data:** Greenfield. Operators delete `.trading-platform-data/market.sqlite` (or point the CLI at a fresh `--market-db`) and re-run the backfill. The previous checkpoint file is not read; a clean v2 checkpoint is written. No migration script is provided — this is an explicit non-goal.
- **Operations:** First run after this change will re-download full 1d history for all active USDT perps. Size budget is the same order as today (hundreds of symbols × years × daily) so a single SQLite file remains fine; the candles table is well-indexed for future multi-symbol queries.
- **Downstream contexts:** Research and Analytics code that consumes `SeriesDescriptor` changes with it. Today those projects reference the kernel struct as `(symbol, timeframe)`; after this change they accept `(InstrumentId, timeframe)` and resolve the display symbol through the registry. The exact call-sites are enumerated during `tasks.md`.
- **Precedes:** Any future Coin-M, spot, or multi-broker work, which now has a clean place to attach without revisiting identity.