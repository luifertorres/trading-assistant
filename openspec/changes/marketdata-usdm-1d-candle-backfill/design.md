## Context

TradingPlatform **MarketData** already provides `ICandleSeriesReader` / `ICandleSeriesWriter` and `SqlitePerSeriesCandleStore` (per-series tables, `TimeFrameCode.Day1` for daily). There is no Binance client in the Platform solution today. The delta spec `**trading-platform-marketdata-binance-1d-backfill`** defines WHAT: universe filter, full `1d` history, **chronological `startTime`/`endTime` paging**, ascending batch writes, broker isolation, CLI entry, **Binance.Net** handling of exchange limits/backoff, checkpoints, optional universe snapshot.

## Goals / Non-Goals

**Goals:**

- Implement a **backfill orchestration** path (primarily **CLI**) that lists active USDT perpetuals from USD-M `exchangeInfo`, then for each symbol fetches **all `1d` klines** using **forward chronological windows** via `**startTime` / `endTime`** (Binance.Net futures klines API), maps to `**OhlcBar`**, and calls `**ICandleSeriesWriter.UpsertAsync`** with `**SeriesDescriptor(symbol, TimeFrameCode.Day1)**` in **ascending batch order** (per spec).
- Add **Binance.Net** only on `**MarketData.Infrastructure`** (and/or CLI composition for client lifetime), with a small **Application-facing port** (e.g. “run backfill” / “list universe”) that does not expose Binance types.
- Provide **rate limiting and backoff via Binance.Net** (per [Binance USD-M general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)), **infinite `HttpClient` request timeout** at registration (long runs must not abort on default timeouts), and a **checkpoint file** (JSON sidecar under the data root) for per-symbol resume.
- Optional: write `**exchangeInfo` snapshot** JSON for the run when enabled.

**Non-Goals:**

- WebSocket live candles, additional intervals, or legacy `TradingAssistant` / Candlestick Data integration.
- Changing the physical per-series table model (ADR-002) or replacing SQLite in this change.
- Multi-broker abstraction beyond what is needed for this single USD-M REST backfill.

## Decisions


| Decision                                      | Rationale                                                                                                                                                                                                                                                                                                                                                                                                                               | Alternatives considered                                                                            |
| --------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| **Symbol universe (locked)**                  | Same triple as **proposal** and delta spec—include a contract **iff** `status` is `**TRADING`**, `**contractType`** is `**PERPETUAL**`, and `**quoteAsset**` is `**USDT**`.                                                                                                                                                                                                                                                             | Broadening scope needs a new proposal/spec delta.                                                  |
| **Checkpoint: single file (v1)**              | **Chosen:** one JSON file updated in place, e.g. `.trading-platform-data/backfill-1d-checkpoint.json` (path configurable with data root). **Schema v1 fields:** `schemaVersion` (`1`), `runId`, `updatedAtUtc`, `marketDatabasePath` (full path string to avoid resuming the wrong DB), `symbols`: array of `{ symbol, complete, lastWrittenOpenTimeMs }` (or equivalent names). Optional later: `universeFingerprint`, `createdAtUtc`. | Per-run files: better audit, more clutter. JSONL: overkill for MVP.                                |
| **Binance interval vs kernel timeframe**      | **Chosen:** use REST interval string `**1d`** only inside **Infrastructure** when calling Binance.Net; always build `**SeriesDescriptor(symbol, TimeFrameCode.Day1)`** and `**OhlcBar`** so Domain/kernel stay on `**1D`**. Centralize mapping in one Infra helper.                                                                                                                                                                     | Library `KlineInterval` enum everywhere: fine later; string `1d` is explicit and matches API docs. |
| **Forward paging with `startTime`/`endTime`** | Each request is the next chronological slice after the last ingested `OpenTime`; aligns with ascending `UpsertAsync` batches.                                                                                                                                                                                                                                                                                                           | Backward paging + reorder: per spec fallback only.                                                 |
| **CLI subcommand on `TradingPlatform.Cli`**   | Long-running, operator-driven; reuses `AddMarketDataSqlite` + DI from `demo`.                                                                                                                                                                                                                                                                                                                                                           | Host `IHostedService`: later.                                                                      |
| **Application port + Infra adapter**          | Orchestration in **Application**; `IBinanceRestClient` / futures API only in **Infrastructure**.                                                                                                                                                                                                                                                                                                                                        | Logic only in CLI: faster spike, weaker layering.                                                  |
| **Sequential symbol loop**                    | Simple ordering and logging; **Binance.Net** enforces exchange rate limits and backoff (no app-level inter-request delay).                                                                                                                                                                                                                                                                                                                | Parallel symbols: needs coordination; still relies on Binance.Net for limits.                    |
| **Binance.Net version**                       | Use the **latest stable** `Binance.Net` on `**MarketData.Infrastructure.csproj**`; reconcile with root `AGENTS.md` / other projects if the repo pins a shared version.                                                                                                                                                                                                                                                                   | Raw HTTP: duplicate work.                                                                          |
| **HTTP client timeout**                       | Register `HttpClient` / `IBinanceRestClient` with **infinite** (or effectively unbounded) **per-request timeout** so long backfill windows are not cut off; rely on **Binance.Net** for exchange rate limits and retry/backoff per [general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info).                                                                                                  | Finite default timeout: spurious failures on slow or large slices.                                 |


## Risks / Trade-offs


| Risk                                           | Mitigation                                                                                                                                      |
| ---------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **Rate limits / bans**                         | **Binance.Net** rate limiting and retry/backoff per Binance rules; **no** separate configurable inter-request delay in app code.                |
| **Checkpoint vs partial symbol**               | After each successful batch: update `lastWrittenOpenTimeMs`; next `startTime` = last written + 1 ms (or next daily boundary per Binance rules). |
| **Single checkpoint file + two jobs**          | Document: use separate `--data-root` (or copy DB) so two processes never share one checkpoint path.                                             |
| **Listing date vs first bar**                  | Empty page → end of history; log short histories.                                                                                               |
| **Clock / UTC**                                | UTC `DateTimeOffset` for API and `OhlcBar`.                                                                                                     |
| `**openspec update` overwriting Cursor files** | Maintainer note only.                                                                                                                           |


## Migration Plan

- **Ship:** New package reference + new types + CLI verb; no change to existing `demo` unless invoked.
- **Run:** Operator runs CLI with DB path; tables created on first `UpsertAsync` as today.
- **Rollback:** Delete checkpoint file and/or restore SQLite backup.

## Open Questions

- None for checkpoint layout or interval mapping (**resolved**—see Decisions). Remaining implementation detail (exact Binance.Net method name on the referenced package version) belongs in **tasks** or code, not design.