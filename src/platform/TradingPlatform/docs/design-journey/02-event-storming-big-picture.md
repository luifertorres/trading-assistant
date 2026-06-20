# Event storming — big picture

## Learning objective

Produce a **whole-platform** event storm: domain events in narrative order, then commands, policies, aggregates, read models, and external systems—so you can **label swimlanes** that become bounded contexts.

## Prerequisites

- [01-problem-space-and-outcomes.md](./01-problem-space-and-outcomes.md)

## Workshop: big-wall flow

### Phase A — Domain events (orange)

Write **past-tense** domain events along the timeline (left → right). Example **seeds only** (replace with your board):

- Candle batch received / Candle bar recorded
- Simulation run started / Simulation run completed
- Runs ranked / Correlation computed
- Portfolio composed / Portfolio definition saved
- Order intent submitted (live path)

### Phase B — Commands and actors

Above the timeline: **commands** (blue) and **actors** who issue them.

### Phase C — Policies

Pink stickies: “When … then …” (e.g. when composing portfolio, enforce max pairwise correlation).

### Phase D — Read models / queries

Green: what must be visible between steps (equity curve, run leaderboard, portfolio file).

### Phase E — External systems

Red: SQLite files, future exchange API, clock.

### Phase F — Hotspots

Black: naming conflicts, unclear ownership, duplicate concepts.

## Workshop: swimlane → candidate context

After the storm, **name swimlanes** (your names first). Then compare to the bounded-context table in [GLOSSARY.md](../GLOSSARY.md).

| Your swimlane name | Maps to glossary context? | Notes / rename |
|--------------------|---------------------------|----------------|
| | MarketData / Research / … | |

## Compare with repo

- Glossary mapping: [GLOSSARY.md](../GLOSSARY.md) — “Bounded contexts (solution mapping)”.
- Solution folders: [TradingPlatform.slnx](../../TradingPlatform.slnx).

## Open questions / ADR candidates

- Is “Analytics” a separate conversation from “Portfolio,” or one policy inside Portfolio on your board?
- Does “Execution” own **routing** only, or also **position state** long term?

## Next doc

[03-subdomains-and-bounded-contexts.md](./03-subdomains-and-bounded-contexts.md)
