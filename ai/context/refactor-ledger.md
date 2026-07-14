# Refactor ledger (high churn)

Single place to record **where new work should go** and what is frozen or legacy. Update this when the migration story changes.

## Current direction

| Bucket | Area | Role | Agent default |
|--------|------|------|----------------|
| `platform` | `src/platform/TradingPlatform/` | **Greenfield** modular monolith (bounded contexts: MarketData, Research, Analytics, Portfolio, Execution, Kernel, Host, Cli). Intended **future primary** codebase. | **Prefer for new features and new architecture.** Read `src/platform/TradingPlatform/AGENTS.md` and `src/platform/TradingPlatform/README.md`. |
| `legacy` | `src/legacy/TradingAssistant/` | **Frozen** single-project live bot (reference / big-ball-of-mud). Restored from pre–Clean Architecture monolith; not layered. | **Maintenance and bugfixes only** unless explicitly asked to extend legacy. Do not add new cross-solution dependencies from Platform → legacy. |
| `mvp` | `src/mvp/Backtesting/` | Isolated backtest MVP | Allowed Binance.Net usage per root `AGENTS.md`; keep isolated from main host. |
| `mvp` | `src/mvp/WebSocketTrading/` | Live WS kline + SMA short worker | Binance.Net **13.1.1** (MVP-only pin); live orders, no dry-run; maintenance in MVP unless porting to Platform Execution. |

## Integration rules

- **No** `ProjectReference` from TradingPlatform to TradingAssistant or either MVP solution (`Backtesting`, `WebSocketTrading`). See `src/platform/TradingPlatform/README.md`.
- When porting behavior, **re-implement or extract** into Platform boundaries rather than linking legacy assemblies.

## OpenSpec

Platform-only. See [openspec/README.md](../../openspec/README.md).

| Track | Main specs | Active changes | Archive |
|-------|------------|----------------|---------|
| **TradingPlatform** | `openspec/specs/trading-platform-*` | `openspec/changes/<name>/` | `openspec/changes/archive/YYYY-MM-DD-*` |

- **New features:** `/opsx:new` with `trading-platform-…` naming; implement under `src/platform/TradingPlatform/`.
- **Legacy live bot:** no OpenSpec track; use [`legacy-port-map.md`](../../src/platform/TradingPlatform/docs/legacy-port-map.md) for porting intent.

## Open migration notes

- **Done on Platform:** MarketData instrument registry, canonical candles store, USD-M 1d backfill (main specs `trading-platform-marketdata-*`); Research backtest CLI (`trading-platform-research-backtest-cli`); dopamine delivery guardrails; first-vector cohort pipeline (Rsi5Extreme / 4H).
- **Port map:** [src/platform/TradingPlatform/docs/legacy-port-map.md](../../src/platform/TradingPlatform/docs/legacy-port-map.md) — feature → context → priority → reward.
