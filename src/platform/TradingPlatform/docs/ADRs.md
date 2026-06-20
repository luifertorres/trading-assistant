# Architecture decision records (greenfield TradingPlatform)

Short-lived notes for the parallel `src/platform/TradingPlatform` tree. Revise as you iterate.

## Contents

- [ADR-001 — Modular monolith](#adr-001--modular-monolith)
- [ADR-002 — SQLite instrument registry + canonical candles table](#adr-002--sqlite-instrument-registry--canonical-candles-table)
- [ADR-003 — Simulation vs live intent sinks](#adr-003--simulation-vs-live-intent-sinks)
- [ADR-004 — Broker ACL stub](#adr-004--broker-acl-stub)
- [ADR-005 — Drawdown correlation](#adr-005--drawdown-correlation)
- [ADR-006 — .NET 10 / C# preview](#adr-006--net-10--c-preview)
- [ADR-007 — Exchange symbols are registry metadata, not domain identity](#adr-007--exchange-symbols-are-registry-metadata-not-domain-identity)

## ADR-001 — Modular monolith

**Decision:** One solution, many assemblies (bounded context per vertical slice), single `TradingPlatform.Host` composition root.

**Rationale:** Matches strategic DDD seams without microservice operational cost for a solo developer.

## ADR-002 — SQLite instrument registry + canonical candles table

**Decision:** `MarketData` persists instruments in an `instruments` registry (stable kernel `InstrumentId`, unique on venue/market/contract type/exchange symbol) and OHLCV rows in a single `candles` table keyed by `(instrument_id, timeframe_id, open_time_ms)`. `SeriesDescriptor` carries `InstrumentId` + `TimeFrameCode`; other contexts depend only on `ICandleSeriesReader` / `ICandleSeriesWriter` and `IInstrumentRegistry`.

**Rationale:** Decouples domain identity from exchange symbol strings and Unicode table-name edge cases; supports multi-venue expansion without revisiting storage layout.

**Supersedes:** Per-series SQLite tables and `SeriesTableNaming` (removed).

## ADR-003 — Simulation vs live intent sinks

**Decision:** `ISimulationOrderIntentSink` (Research) models backtest fills; `ILiveOrderIntentSink` (Execution) models broker I/O. Strategies emit `OrderIntent` via the simulation sink during research; `PortfolioExecutionRouter` adapts to the live sink for execution.

**Rationale:** Same strategy factory (`ITradingStrategyFactory`) for both modes; swap sinks at composition.

## ADR-004 — Broker ACL stub

**Decision:** `BrokerAntiCorruptionStub` and `LoggingLiveOrderIntentSink` stand in for a future Binance (or other) adapter.

**Rationale:** Keeps exchange types out of Domain/Application until a real integration is implemented.

## ADR-005 — Drawdown correlation

**Decision:** Pairwise Pearson correlation on synchronized **underwater** (drawdown-from-peak) fractions of equity curves.

**Rationale:** Simple, deterministic first pass for “drawdown-uncorrelated” portfolio selection; replace or refine in Analytics as needed.

## ADR-006 — .NET 10 / C# preview

**Decision:** Target `net10.0` with `LangVersion=preview` via `Directory.Build.props` to match the main repo stack.

**Rationale:** Consistency with existing trading-assistant projects.

## ADR-007 — Exchange symbols are registry metadata, not domain identity

**Decision:** Raw exchange symbols (including Unicode Binance USD-M names) are stored on registry rows as `exchange_symbol` and resolved for logging or operator snapshots. Domain and kernel types use opaque `InstrumentId` only.

**Rationale:** Supersedes the ADR-007 stopgap that widened per-series table naming; identity now lives in the instrument registry (see ADR-002).
