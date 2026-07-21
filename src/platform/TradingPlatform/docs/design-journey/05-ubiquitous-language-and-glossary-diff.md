# Ubiquitous language and glossary diff

## Learning objective

**Own** the vocabulary: write **your** definitions first, then reconcile with the repo glossary so mismatches become explicit teaching moments (or ADRs).

## Prerequisites

- [04-context-map.md](./04-context-map.md)

## Workshop: term table

Copy and extend. “Action” = keep / rename / merge / split.

| Term | Your definition | Repo definition (from [GLOSSARY.md](../GLOSSARY.md)) | Action |
|------|-----------------|--------------------------------------------------------|--------|
| SeriesDescriptor | | Logical candle stream: symbol + `TimeFrameCode`. | |
| TimeFrameCode | | Chart-style token; note `1m` vs `1M`. | |
| Asset | Canonical `broker:venue:symbol` (e.g. `binance:usdm:BTCUSDT`). | Kernel `Asset`. | keep |
| Direction | `Long` or `Short`. | Kernel `Direction` (was `PositionSide`). | renamed |
| TimeFrame | Chart interval. | `TimeFrameCode`. | keep |
| TradingLogic | Entry/exit rules (e.g. `Sma200Sma5`). | String on `TradingVectorSpec` (was `StrategyKind`). | renamed |
| Trading vector | Unique `(Asset, Direction, TimeFrame, TradingLogic)`. | `TradingVectorSpec` + `TradingVectorId`. | keep |
| VectorInventory | Per-vector tracked open qty. | Kernel type; hedge-mode sibling accounting. | added |
| VectorRiskFraction | Fraction of initial capital per vector. | `SimulationConfiguration` field (was `PositionNotionalFraction`). | renamed |
| Exchange-side position | Broker aggregate for Asset+Direction. | Sum of sibling inventories in hedge mode. | added |
| Simulation run | | One backtest evaluation; config + bars → trades + equity + max drawdown. | |
| Run result | | `SimulationRunResult` persisted for Analytics. | |
| Underwater fraction | | Drawdown-from-peak equity fraction for correlation. | |
| Portfolio definition | | Named set of members (vector id + weight); language to Execution. | |
| Simulation sink | | Accepts `OrderIntent` during backtest. | |
| Live sink | | Same intent shape toward broker. | |
| Broker ACL | | Anti-corruption at exchange boundary. | |

## Workshop: forbidden synonyms

List words you will **not** use interchangeably (e.g. “run” vs “backtest” vs “experiment”).

| Word A | Word B | Your rule |
|--------|--------|-----------|
| | | |

## Compare with repo

- Source of truth for terms: [GLOSSARY.md](../GLOSSARY.md).
- Types that **encode** language: `TradingVectorSpec`, `PortfolioDefinition`, `SimulationRunResult` under `src/platform/TradingPlatform/src/`.

## Open questions / ADR candidates

- If you rename “Trading vector” in speech, does `TradingVectorSpec` stay for historical consistency?
- Should “Run result” include **raw bar replay** reference, or stay equity/trades only?

## Next doc

[06-tactical-ddd-cross-cutting.md](./06-tactical-ddd-cross-cutting.md)
