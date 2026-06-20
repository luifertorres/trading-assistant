# Ubiquitous language — TradingPlatform

| Term | Meaning |
|------|---------|
| **InstrumentId** | Opaque stable identifier for a registered instrument (allocated by the MarketData registry). |
| **SeriesDescriptor** | Logical candle stream: `InstrumentId` + `TimeFrameCode` (e.g. instrument `42` + `1m`). |
| **TimeFrameCode** | Chart-style token: lowercase **s** / **m** (e.g. `1s`, `1m`, `15m`); uppercase **H** / **D** / **W** / **M** (e.g. `1H`, `1D`, `1M`). **1m** = minute, **1M** = month. |
| **Trading vector** | `TradingVectorSpec`: `InstrumentId`, timeframe, position side, strategy kind, parameters, stable `TradingVectorId`. |
| **Simulation run** | One backtest evaluation: config + bars → trades + equity + max drawdown. |
| **Run result** | `SimulationRunResult` persisted for Analytics (JSON payload in `SimulationRuns` table). |
| **Underwater fraction** | Per-bar drawdown from running peak equity, used for correlation in Analytics. |
| **Portfolio definition** | Named, versioned set of `PortfolioMember` (vector id + weight) — published language to Execution. |
| **Simulation sink** | Accepts `OrderIntent` during a backtest; updates virtual PnL and trade list. |
| **Live sink** | Accepts the same `OrderIntent` shape for real or paper broker placement (stub today). |
| **Broker ACL** | Anti-corruption layer at the exchange boundary; no broker types in inner Domain. |

## Bounded contexts (solution mapping)

| Context | Projects |
|---------|----------|
| BuildingBlocks | `TradingPlatform.Kernel` |
| MarketData | `MarketData.Domain`, `MarketData.Application`, `MarketData.Infrastructure` |
| Research | `Research.Domain`, `Research.Application`, `Research.Infrastructure` |
| Analytics | `Analytics.Domain`, `Analytics.Application`, `Analytics.Infrastructure` |
| Portfolio | `Portfolio.Domain`, `Portfolio.Application`, `Portfolio.Infrastructure` |
| Execution | `Execution.Domain`, `Execution.Application`, `Execution.Infrastructure` |
| Delivery | `TradingPlatform.Host`, `TradingPlatform.Cli` |
