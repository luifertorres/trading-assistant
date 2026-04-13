# Routing map (high churn)

**Route by reasoning** (intent, blast radius, dependencies), not keywords. Use this file when a task is **ambiguous**, **cross-cutting**, or spans **multiple** bounded contexts or solutions.

For **fast / routine** work, **bypass** this document: go straight to the right `AGENTS.md`, path-scoped rule, or slash command (see **Bypass** below).

## Default: which solution?

| Situation | Read first | Notes |
|-----------|------------|--------|
| New feature, new module, DDD/context work | `src/TradingPlatform/AGENTS.md`, `src/TradingPlatform/README.md` | No references to legacy solutions. |
| Bugfix or small change in existing bot | Matching layer under `src/TradingAssistant/**/AGENTS.md` | Legacy path; see root `AGENTS.md` for EF and Binance boundaries. |
| Backtesting CLI / MVP only | `src/Backtesting/README.md` | Isolated from main host. |
| OpenSpec change already named and scoped | `openspec/changes/<name>/` artifacts | Use bypass commands; do not re-plan from scratch unless blocked. |

## Cross-cutting “paired change” hints

| You touch | Also consider |
|-----------|----------------|
| Binance REST/WS or exchange DTOs | `.cursor/rules/binance-net.mdc`, `TradingAssistant.Infrastructure/AGENTS.md` (legacy) or Platform Execution/MarketData infra |
| Domain public types or invariants | Same layer `AGENTS.md`; broker-agnostic contracts |
| EF schema or persistence | Migrations policy in root `AGENTS.md`; context-owned persistence in Platform |
| OpenSpec capability wording | `openspec/specs/<capability>/spec.md` plus the active change delta specs |

## Modular context pack (rate of change)

- Stable norms: [engineering-principles.md](./engineering-principles.md)
- Domain vocabulary: [trading-domain.md](./trading-domain.md)
- Migration defaults: [refactor-ledger.md](./refactor-ledger.md)
- Routing mistakes: [routing-overrides.md](./routing-overrides.md)

---

## Bypass: OpenSpec slash commands

Use a **direct command** when you already know the workflow—**zero extra routing**.

| Command | Use when |
|---------|-----------|
| `/opsx:new` | Starting a **new** OpenSpec change from scratch. |
| `/opsx:ff` | You want the full artifact set for a change quickly (fast-forward). |
| `/opsx:continue` | Continuing artifact creation for an **existing** change. |
| `/opsx:apply` | Implementing tasks for a **named** change (`openspec/changes/<name>/`). |
| `/opsx:verify` | Validating implementation against change artifacts **before** archive. |
| `/opsx:sync` | Syncing delta specs from a change into **main** specs without archiving. |
| `/opsx:archive` | Archiving **one** completed change. |
| `/opsx:bulk-archive` | Archiving **multiple** completed changes. |
| `/opsx:explore` | **Thinking / clarification only**—no implementation; optional OpenSpec capture. |
| `/opsx:onboard` | Guided OpenSpec onboarding cycle. |

**Sequencing (typical):** explore or spec → apply → verify → (sync if needed) → archive.

**Ambiguous?** If you are unsure whether to use OpenSpec at all, read `.cursor/commands/opsx-explore.md` stance or run `/opsx:explore` before `/opsx:apply`.

## Bypass: skills (direct invocation)

Invoke by name when the task clearly matches (see each `SKILL.md`):

- `binance-net` — USD-M Binance.Net usage and boundaries.
- `dotnet-test` — running / fixing .NET tests.
- `openspec-*` — new/continue/apply/verify/archive/sync/ff/onboard/explore/bulk-archive per description.
- `commit` / `pr` — git hygiene when requested.
- `chief-of-staff` — short **preflight** only (goal, audience, sequence, stop conditions); not for bulk implementation.
