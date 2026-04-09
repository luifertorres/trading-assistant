# Architecture decision records (greenfield TradingPlatform)

Short-lived notes for the parallel `src/TradingPlatform` tree. Revise as you iterate.

## ADR-001 — Modular monolith

**Decision:** One solution, many assemblies (bounded context per vertical slice), single `TradingPlatform.Host` composition root.

**Rationale:** Matches strategic DDD seams without microservice operational cost for a solo developer.

## ADR-002 — SQLite + per-series candle tables

**Decision:** `MarketData` persists each `(Symbol, TimeFrameCode)` series in its own physical table; naming is internal to `MarketData.Domain.SeriesTableNaming`.

**Rationale:** Aligns with chart-style table names (e.g. `BTCUSDT_1m`, `BTCUSDT_1M`, `ETHUSDT_1D`); other contexts depend only on `ICandleSeriesReader` / `ICandleSeriesWriter`.

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
