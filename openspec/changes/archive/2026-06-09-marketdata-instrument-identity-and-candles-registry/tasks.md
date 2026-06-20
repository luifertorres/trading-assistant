# Tasks — marketdata-instrument-identity-and-candles-registry

## 1. Kernel identity types

[x] 1.1 Add `InstrumentId` (`readonly record struct` over `long`) to `src/platform/TradingPlatform/src/BuildingBlocks/TradingPlatform.Kernel/InstrumentId.cs` with equality and `ToString()` suitable for logging.
[x] 1.2 Change `SeriesDescriptor` to `(InstrumentId Instrument, TimeFrameCode TimeFrame)`; update `Validate()` to require `Instrument.Value > 0` and non-empty timeframe.
[x] 1.3 Change `TradingVectorSpec` to carry `InstrumentId Instrument` instead of `string Symbol`; update `Series` to `new SeriesDescriptor(Instrument, TimeFrame)`.
[x] 1.4 Update `tests/MarketData.Domain.Tests/SeriesDescriptorValidationTests.cs` for the new `SeriesDescriptor` shape (invalid/zero `InstrumentId`, valid instrument).

## 2. MarketData domain and application ports

[x] 2.1 Add `Instrument` entity and broker-agnostic `InstrumentUpsert` record in `MarketData.Domain` / `MarketData.Application` with all registry fields from [design.md](design.md).
[x] 2.2 Add `IInstrumentRegistry` (`UpsertAsync`, `GetByIdAsync`, `GetByExchangeSymbolAsync`, `ListAllAsync`) in `MarketData.Application`.
[x] 2.3 Add `BrokerFetchHandle` opaque struct and `UsdMInstrumentListing` DTO in `MarketData.Application`.
[x] 2.4 Replace `IUsdM1dBackfillExchange.GetActiveUsdtPerpetualSymbolsAsync` with `ListUsdtPerpetualInstrumentsAsync` returning listings; change `GetDailyKlinesPageAsync` to accept `BrokerFetchHandle` instead of `string symbol`.
[x] 2.5 Add `BackfillCheckpointDocumentV2` and `BackfillCheckpointInstrumentEntryV2` keyed by `InstrumentId`; keep v1 types only if still referenced by tests during transition, then remove.
[x] 2.6 Update `IBackfillCheckpointStore` / `BackfillCheckpointJsonStore` to load only `schemaVersion == 2` (log and treat v1 as no checkpoint).

## 3. SQLite infrastructure — shared database bootstrap

[x] 3.1 Add shared SQLite bootstrap (single `SqliteConnection` per database path) that creates `instruments`, `timeframes`, and `candles` DDL on first open and idempotently seeds `timeframes` for at least `TimeFrameCode.Day1`.
[x] 3.2 Implement `SqliteInstrumentRegistry` with upsert on `(venue, market, contract_type, exchange_symbol)` returning stable `InstrumentId`; preserve `first_seen_utc` on conflict, update `last_seen_utc` and metadata.
[x] 3.3 Implement `SqliteCandleStore` as `ICandleSeriesReader` + `ICandleSeriesWriter`: resolve `SeriesDescriptor` to `(instrument_id, timeframe_id)`, upsert/read with bound parameters only, ascending `open_time_ms` order, `ON CONFLICT` upsert semantics.
[x] 3.4 Update `ServiceCollectionExtensions.AddMarketDataSqlite` to register `IInstrumentRegistry`, `ICandleSeriesReader`, and `ICandleSeriesWriter` against the new implementations (shared connection).
[x] 3.5 Delete `SqlitePerSeriesCandleStore.cs`, `SeriesTableNaming.cs`, and `tests/MarketData.Domain.Tests/SeriesTableNamingTests.cs`.

## 4. Binance USD-M adapter and orchestration

[x] 4.1 Update `BinanceUsdM1dBackfillExchange` to map `BinanceFuturesUsdtSymbol` → `InstrumentUpsert` + `BrokerFetchHandle` with venue constants `binance` / `usdm` / `perpetual`; serialize filters to `filters_json`.
[x] 4.2 Unwrap `BrokerFetchHandle` inside `GetDailyKlinesPageAsync` for Binance REST calls only (no symbol strings in Application orchestration).
[x] 4.3 Rewire `Usdm1dBackfillOrchestrator`: registry upsert before kline loop, iterate `InstrumentId`s, build `SeriesDescriptor(instrumentId, Day1)`, remove `SeriesTableNaming` usage, log human-readable `exchange_symbol` via registry on errors.
[x] 4.4 Sync checkpoint `instruments[]` by `InstrumentId` after universe refresh (drop stale entries, add new ones).
[x] 4.5 Extend `WriteExchangeInfoSnapshotAsync` to also write `instrument-ids-{runId}.json` under `dataRoot` mapping `exchange_symbol` → `instrumentId` for instruments upserted in that run; document the path in CLI help or README.

## 5. Downstream compile fixes

[x] 5.1 Update `src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli/Program.cs` demo paths: use `InstrumentId` in `SeriesDescriptor` and `TradingVectorSpec` (hard-code placeholder IDs or resolve from registry after a seed upsert for the demo).
[x] 5.2 Update Research call sites (`BarProcessingContext`, `IBacktestRunner`, `ITradingStrategyFactory`, `DefaultTradingStrategyFactory`) to use `InstrumentId` / new `TradingVectorSpec` shape.
[x] 5.3 Update `Execution.Application/PortfolioExecutionRouter.cs` if it references `TradingVectorSpec.Symbol` or string-based `SeriesDescriptor`.
[x] 5.4 Grep `src/platform/TradingPlatform` for remaining `SeriesDescriptor(` string constructors, `SeriesTableNaming`, and `SqlitePerSeriesCandleStore`; fix or remove all hits.

## 6. Tests

[x] 6.1 Add infrastructure tests for registry upsert idempotency (same natural key → same `InstrumentId`; distinct keys → distinct IDs).
[x] 6.2 Add infrastructure tests for `SqliteCandleStore` upsert conflict and ascending read order on `(instrument_id, timeframe_id, open_time_ms)`.
[x] 6.3 Update `tests/MarketData.Application.Tests/Usdm1dBackfillOrchestratorTests.cs` for registry + listing/handle fakes, v2 checkpoint resume/skip, and v1 checkpoint ignored.
[x] 6.4 Run `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx` and `dotnet test` on MarketData test projects; fix failures.

## 7. Documentation and ADRs

[x] 7.1 Supersede ADR-002 in `src/platform/TradingPlatform/docs/ADRs.md` (canonical `candles` table + instrument registry); remove or narrow ADR-007 stopgap note.
[x] 7.2 Update `src/platform/TradingPlatform/docs/GLOSSARY.md` and relevant `design-journey` references for `InstrumentId`, `SeriesDescriptor`, and retired per-series tables.
[x] 7.3 Document operator greenfield steps in `src/platform/TradingPlatform/README.md`: delete `market.sqlite` and v1 `backfill-1d-checkpoint.json`, re-run `backfill-1d`, expect full re-download; note v2 checkpoint and snapshot companion file.
