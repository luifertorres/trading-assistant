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

## Open migration notes

- Edit this section when a capability is considered **done** on Platform or **frozen** on legacy.
