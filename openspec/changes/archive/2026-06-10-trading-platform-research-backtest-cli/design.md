## Context

TradingPlatform bounded contexts already implement the core simulation loop:

```
IInstrumentRegistry ──▶ InstrumentId
ICandleSeriesReader ──▶ OhlcBar[] (Day1 from market.sqlite)
IBacktestRunner ──▶ SimulationRunResult (via ITradingStrategyFactory + SimulationOrderIntentSink)
ISimulationRunRepository ──▶ research.sqlite (optional persist)
```

The `demo` CLI command bypasses real data by writing synthetic 1m bars and running two hard-coded `FixedWindow` vectors. The `backfill-1d` command populates `market.sqlite` with real USD-M daily history for the full USDT perpetual universe. The missing piece is a **delivery command** that connects backfill output to `IBacktestRunner` without synthetic seeding.

Isolated `src/mvp/Backtesting/Backtesting.Mvp` already supports live Binance 1h fetches with a hard-coded RSI engine; that path is **out of scope** — this change stays on the greenfield Platform stack per refactor ledger.

## Goals / Non-Goals

**Goals:**

- Add `TradingPlatform.Cli backtest` that runs one simulation against **persisted** `Day1` candles for a given exchange symbol (default `BTCUSDT`).
- Resolve `InstrumentId` via `IInstrumentRegistry.GetByExchangeSymbolAsync` (venue `binance`, market `usdm`, contract `perpetual`) — same natural key the backfill uses.
- Pass optional `--from` / `--to` (UTC `DateTimeOffset`) into `BacktestRequest`; when omitted, use all bars available in the store for that series.
- Support `FixedWindow` strategy kind with `--enter-bar` and `--exit-bar` parameters (0-based bar indices), matching `FixedWindowStrategy` today.
- Accept simulation config flags: `--initial-capital`, `--fee-bps`, `--position-fraction` (defaults aligned with `demo`).
- Print summary to stdout; support `--save` to persist via `ISimulationRunRepository`.
- Fail fast with actionable messages when instrument is missing, series has zero bars, or strategy kind is unknown.

**Non-Goals:**

- New indicator strategies (RSI, legacy host strategies) — follow-on change after pipeline proof.
- Additional timeframe backfill (1h, 1m) or live WebSocket ingest.
- Changes to `BacktestRunner`, `SimulationOrderIntentSink`, or MarketData storage contracts unless a bug blocks wiring.
- Portfolio composition, analytics ranking, or execution router (remain in `demo`).
- Project references to `src/mvp/Backtesting/` or `src/legacy/TradingAssistant/`.

## Decisions

### 1. CLI verb and argument shape

**Chosen:**

```text
TradingPlatform.Cli backtest
  --market-db <path>          # required; same file as backfill-1d
  [--research-db <path>]      # default: <cwd>/.trading-platform-data/research.sqlite
  --symbol <exchangeSymbol>   # default: BTCUSDT
  [--from <iso8601>] [--to <iso8601>]
  --strategy FixedWindow      # only kind in MVP
  --enter-bar <int> --exit-bar <int>
  [--initial-capital <decimal>] [--fee-bps <decimal>] [--position-fraction <decimal>]
  [--save]                    # persist SimulationRunResult
```

**Rationale:** Mirrors `backfill-1d` flag style (`--market-db`, `--data-root` pattern); explicit symbol keeps operator mental model aligned with exchange listings and backfill logs.

**Alternatives:** Positional symbol only (less self-documenting); require `--instrument-id` (harder for operators).

### 2. Timeframe fixed to Day1

**Chosen:** Hard-require `TimeFrameCode.Day1` on the `SeriesDescriptor` built for the run.

**Rationale:** Only timeframe currently backfilled; avoids silent empty runs when operator expects 1h data.

**Alternatives:** `--timeframe` flag with validation against store contents (deferred until multi-interval backfill exists).

### 3. Instrument resolution

**Chosen:** `IInstrumentRegistry.GetByExchangeSymbolAsync("binance", "usdm", "perpetual", symbol)`; error if not found with message suggesting `backfill-1d` first.

**Rationale:** Registry is source of truth after backfill universe refresh; no ad-hoc symbol strings in Research layer.

### 4. DI composition for `backtest` command

**Chosen:** Separate `ServiceCollection` bootstrap (same pattern as `backfill-1d` and `demo`):

- `AddMarketDataSqlite(marketDb)`
- `AddResearchInfrastructure(researchDb)` when `--save` or always (lightweight; enables future analytics hooks)

Do **not** register Analytics/Portfolio/Execution for this command.

**Rationale:** Minimal surface, fast startup, clear dependency boundary.

### 5. Output contract

**Chosen:** Log lines at Information level:

- Resolved `InstrumentId`, bar count, date range actually used
- `RunId`, `TotalTrades`, `FinalEquity`, `MaxDrawdownFraction` (formatted as percent)
- When `--save`, confirm research DB path

No CSV equity export in this change (can add `--csv` later).

### 6. Empty data behavior

**Chosen:** If `ICandleSeriesReader.ReadAsync` returns zero bars, exit non-zero with message distinguishing:

- instrument not in registry
- instrument exists but no `Day1` bars (suggest backfill or widen date range)

`BacktestRunner` already returns a zero-trade result for empty bars; CLI should **pre-check** or treat zero-bar result as failure for operator clarity.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Operator runs backtest before backfill | Clear error + README workflow |
| `FixedWindow` bar indices exceed series length | Document 0-based indices; strategy simply won't trade |
| Large symbol universe irrelevant to CLI | CLI targets one symbol; no full-universe scan |
| Research DB path confusion vs demo defaults | Document defaults in README; allow `--research-db` override |

## Migration Plan

1. Ship `backtest` command (no breaking changes to existing verbs).
2. Update `src/platform/TradingPlatform/README.md` with two-step workflow: `backfill-1d` → `backtest`.
3. Optionally add integration test with in-memory/SQLite fixture bars (no live network).

**Rollback:** Remove command handler; no schema migrations.

## Open Questions

- Should `--save` become default-on once analytics workflows land? (Defer; default off for quick experiments.)
- Add `--list-symbols` helper on registry? (Nice-to-have; not blocking.)
