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
- [ADR-008 — First live vector cohort (guarded)](#adr-008--first-live-vector-cohort-guarded)

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

**Status (2026-06):** Partially superseded by [ADR-008](#adr-008--first-live-vector-cohort-guarded). `BinanceLiveOrderIntentSink` is registered when `AddExecutionInfrastructure(liveTrading: true)`; default host/CLI paths remain disarmed with `LoggingLiveOrderIntentSink`. Full portfolio-gated live routing and position follow-up are still deferred (plan todo `slice-management`).

## ADR-005 — Drawdown correlation

**Decision:** Pairwise Pearson correlation on synchronized **underwater** (drawdown-from-peak) fractions of equity curves.

**Rationale:** Simple, deterministic first pass for “drawdown-uncorrelated” portfolio selection; replace or refine in Analytics as needed.

## ADR-006 — .NET 10 / C# preview

**Decision:** Target `net10.0` with `LangVersion=preview` via `Directory.Build.props` to match the main repo stack.

**Rationale:** Consistency with existing trading-assistant projects.

## ADR-007 — Exchange symbols are registry metadata, not domain identity

**Decision:** Raw exchange symbols (including Unicode Binance USD-M names) are stored on registry rows as `exchange_symbol` and resolved for logging or operator snapshots. Domain and kernel types use opaque `InstrumentId` only.

**Rationale:** Supersedes the ADR-007 stopgap that widened per-series table naming; identity now lives in the instrument registry (see ADR-002).

## ADR-008 — First live vector cohort (guarded)

**Context:** Legacy `Rsi5ExtremeStrategy` gave fast feedback (live orders) but lacked Platform boundaries and hard real-money caps. Greenfield needed an end-to-end slice that reuses ADR-003 (shared strategy, swapped sinks) without growing legacy references.

**Decision:**

1. **Cohort:** Four USD-M perpetual vectors — DOGEUSDT, XRPUSDT, SOLUSDT, 1000PEPEUSDT — each `(long, 4H, Rsi5Extreme, 1x ISOLATED)`.
2. **Strategy (Research):** `Rsi5ExtremeStrategy` implements `ITradingStrategy` with Skender RSI(5); entry = cross up through 10; SL = min low of last 6 bars (24h on 4H); exit at 1x = fixed **8% price-move TP** and/or **RSI ≥ 70** (legacy leverage-based +100% TP is unreachable at 1x).
3. **Data (MarketData):** `UsdmBackfillOrchestrator` accepts `TimeFrameCode` + optional symbol filter; `backfill-4h` CLI seeds SQLite. `ILiveCandleFeed` + `BinanceLiveCandleFeed` emit closed candles; `LiveStrategyWorker` replays the last closed bar at startup.
4. **Gate (Research → Execution):** `BacktestVerdict` JSON per symbol (`cohort-backtest` CLI); live arming blocked unless verdict `Pass` (min trades, return, max DD, profit factor).
5. **Portfolio (Analytics + Portfolio):** `cohort-compose` chains `RunAnalyticsEngine` + `PortfolioComposer` (ADR-005) on PASS runs; persists selected set as portfolio JSON.
6. **Execution:** `BinanceLiveOrderIntentSink` behind `ILiveOrderIntentSink` when `liveTrading: true`; `LiveTradingOptions` enforces disarmed default, 1x leverage, ISOLATED margin, `MARKET_LOT_SIZE` sizing, per-symbol/total notional caps, daily order cap, kill-switch file. `fire-test-order --arm` is the guarded manual proof path.
7. **Process (repo):** Delivery guardrails (`delivery-principles.md`, `/slice`, `WINS.md`, scoped context `AGENTS.md`, `live-trading-safety.mdc`) and [legacy-port-map.md](./legacy-port-map.md) sequence future ports.

**Rationale:** One shared strategy + sink swap keeps backtest and live aligned at 1x. Verdict + portfolio JSON + explicit arming prevent impulsive real-money orders. Tiny notional (~$5/symbol) caps blast radius on a ~$102 account.

**Consequences:**

- `OrderIntent` carries optional `StopLossPrice`, `TakeProfitPrice`, `ExitPrice` for simulation and live SL placement.
- Host still uses logging live sink by default; CLI opts into Binance adapter for `fire-test-order`.
- **Deferred** (plan todo #12 `slice-management`): user-data stream, SL/TP follow-up managers, Telegram — see legacy port map P1 items.

**References:** ADR-003, [legacy-port-map.md](./legacy-port-map.md), `.cursor/rules/live-trading-safety.mdc`.
