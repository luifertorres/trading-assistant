# Subdomains and bounded contexts

## Learning objective

Classify **subdomains** (core / supporting / generic) and justify **context boundaries** by linguistic seams, rate of change, and ownership—not by folder convenience.

## Prerequisites

- [02-event-storming-big-picture.md](./02-event-storming-big-picture.md)

## Workshop: subdomain table

| Area (your language) | Type: core / supporting / generic | Why |
|----------------------|-----------------------------------|-----|
| Historical candles | | |
| Simulation / backtest | | |
| Run metrics / correlation | | |
| Portfolio construction | | |
| Broker execution | | |

## Workshop: boundary justification

For **each** context in the solution, answer:

1. **What term set is authoritative here?**
2. **What would change together on one feature branch?**
3. **What integration is “translation” vs “shared model”?**

Fill:

| Context (solution name) | Authoritative terms | Changes with… | Keep separate from … because … |
|-------------------------|---------------------|---------------|----------------------------------|
| MarketData | | | |
| Research | | | |
| Analytics | | | |
| Portfolio | | | |
| Execution | | | |
| TradingPlatform.Kernel | | | |
| Delivery (Host, Cli) | | | |

## Compare with repo

- Modular monolith decision: [ADRs.md](../ADRs.md) **ADR-001**.
- Physical mapping: [TradingPlatform.slnx](../../TradingPlatform.slnx) folders under `/src/`.
- [GLOSSARY.md](../GLOSSARY.md) — bounded context → projects.

## Open questions / ADR candidates

- Should **Analytics** stay downstream of Research only, or also accept external run imports (CSV)?
- Is **Kernel** a true **Shared Kernel** (co-owned) or a **Published Language** library maintained by one team-of-one?

## Next doc

[04-context-map.md](./04-context-map.md)
