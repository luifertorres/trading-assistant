# Delivery principles (dopamine-driven)

Norms for agent sessions on this repo. Goal: **one visible, same-session win** per session while staying on the path to profitable live trading.

## Core rules

1. **Same-session reward** — Every session ends with a runnable demo and an entry in [WINS.md](../../WINS.md). If you cannot demo it, the slice is too big; cut it.
2. **Foresight** — Each win is a step on the real vector pipeline (data → backtest gate → portfolio → live feed → tiny orders), not a detour or parallel system.
3. **Define proof up front** — Before coding, state the visible artifact and the exact command that proves it.
4. **Timebox** — Prefer the smallest slice that ships today. Defer "later" items explicitly (e.g. position management, Telegram).
5. **Stop-and-show** — After the proof command passes, stop and show output; append WINS.md; do not silently continue to the next phase unless asked.
6. **Token economy** — Load only the scoped `AGENTS.md` and rules for the context you touch (MarketData, Research, Execution, Hosts). Do not re-read the whole monolith each turn.
7. **Real-money safety** — Live order code follows [`ai/templates/rules/live-trading-safety.mdc`](../templates/rules/live-trading-safety.mdc). Disarmed by default; arming is explicit per session.

## Session ritual

Use the **ship-a-slice** skill ([`ai/skills/ship-a-slice/SKILL.md`](../skills/ship-a-slice/SKILL.md)) or `/slice` command to pick one slice, list minimal steps, run the demo, and log the win.

## What counts as a win

| Good | Bad |
|------|-----|
| CLI prints PASS/FAIL with numbers | "Implemented but not run" |
| File exists and is linked from routing | Orphan doc nobody reads |
| Order visible in Binance Desktop (armed, capped) | Config-only with no proof |
| Backtest verdict JSON on disk | Synthetic-only demo when real data was the goal |

## Related

- [routing-map.md](./routing-map.md) — bypass commands and paired-change hints
- [refactor-ledger.md](./refactor-ledger.md) — Platform vs legacy; [legacy-port-map.md](../../src/platform/TradingPlatform/docs/legacy-port-map.md) for feature order
- [engineering-principles.md](./engineering-principles.md) — dependency and broker boundaries
