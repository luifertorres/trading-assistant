# Refactor ledger (high churn)

Single place to record **where new work should go** and what is frozen or legacy. Update this when the migration story changes.

## Current direction

| Area | Role | Agent default |
|------|------|----------------|
| `src/TradingPlatform/` | **Greenfield** modular monolith (bounded contexts: MarketData, Research, Analytics, Portfolio, Execution, Kernel, Host, Cli). Intended **future primary** codebase. | **Prefer for new features and new architecture.** Read `src/TradingPlatform/AGENTS.md` and `src/TradingPlatform/README.md`. |
| `src/TradingAssistant/` | **Legacy** Clean Architecture monolith (single product). Becomes **reference / big-ball-of-mud** over time as Platform matures. | **Maintenance and bugfixes only** unless explicitly asked to extend legacy. Do not add new cross-solution dependencies from Platform → legacy. |
| `src/Backtesting/` | Isolated MVP tooling | Allowed Binance.Net usage per root `AGENTS.md`; keep isolated from main host patterns unless migrating into Platform. |

## Integration rules

- **No** `ProjectReference` from TradingPlatform to TradingAssistant (see `src/TradingPlatform/README.md`).
- When porting behavior, **re-implement or extract** into Platform boundaries rather than linking legacy assemblies.

## OpenSpec tracks

| Track | Main specs | Active changes | Archive |
|-------|------------|----------------|---------|
| **TradingPlatform** (primary) | `openspec/specs/trading-platform-*` | `openspec/changes/<name>/` — new work only | `openspec/changes/archive/YYYY-MM-DD-*` |
| **Legacy reference** | `architecture`, `trading-strategies`, `candlestick-*`, etc. (see [openspec/README.md](../../openspec/README.md)) | None — do not revive archived legacy proposals | `openspec/changes/archive/2026-06-09-legacy-*`, `2026-06-09-cqrs-candle-repository` |

- **New features:** `/opsx:new` with `trading-platform-…` naming; implement under `src/TradingPlatform/`.
- **Legacy live bot:** read archived changes + `src/TradingAssistant/`; extend legacy main specs only for maintenance, not greenfield.

## Open migration notes

- **Done on Platform:** MarketData instrument registry, canonical candles store, USD-M 1d backfill (main specs `trading-platform-marketdata-*`).
- **Frozen on legacy:** OpenSpec proposals for clean-architecture, multi-broker, event-driven-dca, risk-v2, backtesting-module (archived with `LEGACY.md`).
- **In progress on Platform:** Research simulation / backtest CLI (`trading-platform-research-backtest-cli` change when active).
