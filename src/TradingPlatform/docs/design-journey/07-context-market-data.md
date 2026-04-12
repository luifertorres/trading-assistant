# Context deep dive — MarketData

## Learning objective

Understand **candle series** as the core concept: identity, persistence boundaries, read/write ports, and how other contexts depend on **abstractions** only.

## Strategic recap

- **Subdomain:** supporting (foundational data) unless your differentiation is data acquisition itself.
- **Upstream/downstream:** upstream to Research and Execution paths that consume bars; downstream from external market feeds (not fully implemented in greenfield MVP).

## Prerequisites

- [06-tactical-ddd-cross-cutting.md](./06-tactical-ddd-cross-cutting.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | Upsert overlapping bars for same `OpenTime` | Row updated (idempotent on key) |
| 2 | Read range `from`–`to` | Ordered `OhlcBar` list |
| 3 | Unknown series | Table created on first write (current infra behavior—validate if you agree) |
| 4 | Invalid symbol/timeframe combo | (define: reject vs sanitize vs allow) |

## Model sketch

- **Entities / VOs:** `SeriesDescriptor`, `OhlcBar` (in Kernel today), physical table naming internal to MarketData.
- **Aggregate / consistency boundary (your call):**
  - Option A: **One aggregate = CandleSeries** keyed by `SeriesDescriptor`; invariant “no two bars with same open time” inside series.
  - Option B: **Series is not a classic aggregate**; SQLite table + upsert is the source of truth; domain is thin.
- **Invariants to state explicitly:** monotonic open times? gap tolerance? timezone?

## Application ports

| Port | Responsibility |
|------|------------------|
| `ICandleSeriesWriter` | Persist / upsert bars for a series |
| `ICandleSeriesReader` | Query bars by open-time range |

## Infrastructure choices

- **SQLite file** per deployment slice; **per-series physical table** — see [ADRs.md](../ADRs.md) **ADR-002**.
- Table naming: [SeriesTableNaming.cs](../../src/MarketData/MarketData.Domain/SeriesTableNaming.cs) (not exposed outside MarketData).

## Compare with repo

| Artifact | Path |
|----------|------|
| Reader/Writer interfaces | [ICandleSeriesReader.cs](../../src/MarketData/MarketData.Application/ICandleSeriesReader.cs), [ICandleSeriesWriter.cs](../../src/MarketData/MarketData.Application/ICandleSeriesWriter.cs) |
| SQLite store | [SqlitePerSeriesCandleStore.cs](../../src/MarketData/MarketData.Infrastructure/SqlitePerSeriesCandleStore.cs) |
| DI registration | [MarketData.Infrastructure/ServiceCollectionExtensions.cs](../../src/MarketData/MarketData.Infrastructure/ServiceCollectionExtensions.cs) |
| Kernel shapes used | [SeriesDescriptor.cs](../../src/BuildingBlocks/TradingPlatform.Kernel/SeriesDescriptor.cs), [OhlcBar.cs](../../src/BuildingBlocks/TradingPlatform.Kernel/OhlcBar.cs), [TimeFrameCode.cs](../../src/BuildingBlocks/TradingPlatform.Kernel/TimeFrameCode.cs) |

**Mini event storm (this context only):** commands/events for ingest/query; compare to `UpsertAsync` / `ReadAsync`.

## Open questions / ADR candidates

- Challenge ADR-002 if you prefer **one table** + series column for operational simplicity.
- Should **feeds** (WebSocket ingest) live in MarketData.Infrastructure or a future Delivery worker?

## Next doc

[08-context-research.md](./08-context-research.md)
