# Ubiquitous language — TradingPlatform

Aligned with Ivan Scherman trading-vector language (see WebSocketTrading MVP README).

| Term | Meaning |
|------|---------|
| **Asset** | Canonical broker identity `broker:venue:symbol` (e.g. `binance:usdm:BTCUSDT`). Kernel type `Asset`. |
| **Direction** | `Long` or `Short`. Kernel enum `Direction`. |
| **TimeFrame** | Chart interval token. Kernel type `TimeFrameCode` (e.g. `1D`, `4H`, `5m`). |
| **TradingLogic** | Entry/exit rules (e.g. `Sma200Sma5`, `Rsi5Extreme`). String on `TradingVectorSpec`. |
| **TradingVector** | Unique `(Asset, Direction, TimeFrame, TradingLogic)`. Kernel type `TradingVectorSpec` + stable `TradingVectorId`. |
| **VectorInventory** | Per-vector tracked open quantity. Enter → `AddFill`; exit → `ConsumeForExit`. Sibling vectors on same Asset+Direction sum on the exchange side in hedge mode. |
| **VectorRiskFraction** | Fraction of `InitialCapital` allocated per vector (e.g. `0.02` = 2%). Independent per vector. |
| **Exchange-side position** | Broker aggregate for `(Asset, Direction)` in hedge mode = sum of sibling `VectorInventory` quantities. |
| **InstrumentId** | Opaque stable identifier for a registered instrument (allocated by the MarketData registry). |
| **SeriesDescriptor** | Logical candle stream: `InstrumentId` + `TimeFrameCode`. |
| **Simulation run** | One backtest evaluation: config + bars → trades + equity + max drawdown. |
| **Run result** | `SimulationRunResult` persisted for Analytics (JSON payload in `SimulationRuns` table). |
| **Underwater fraction** | Per-bar drawdown from running peak equity, used for correlation in Analytics. |
| **Portfolio definition** | Named, versioned set of `PortfolioMember` (vector id + weight) — published language to Execution. |
| **Simulation sink** | Accepts `OrderIntent` during a backtest; updates virtual PnL and trade list. |
| **Live sink** | Accepts the same `OrderIntent` shape for real or paper broker placement. |
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
