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
| Trading vector | | `TradingVectorSpec` + stable `TradingVectorId`. | |
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
