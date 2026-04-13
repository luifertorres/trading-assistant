---
name: chief-of-staff
description: Short preflight for ambiguous work—clarify intent, audience, sequencing across Platform vs legacy and OpenSpec, then hand off. Use when the user asks you to route, plan, or de-risk before touching code.
---

# Chief of Staff (preflight only)

This skill is **not** for implementing features. It produces a **small routing decision** so execution stays fast and avoids the “switchboard operator” trap.

## When to use

- The request spans **multiple** layers, contexts, or solutions (TradingPlatform vs legacy TradingAssistant).
- It is unclear whether **OpenSpec** (`/opsx:*`) is required.
- You need a **sequence** of which `AGENTS.md` / rules to load before editing.

## Preflight checklist (answer briefly, then act or ask)

1. **Intent** — What outcome and constraints (risk, deadline, “do not migrate legacy,” etc.)?
2. **Identity / audience** — Who consumes the result (runtime operator, reviewer, future you)? What would look obviously wrong if missing?
3. **Blast radius** — Domain-only, single BC, cross-BC, exchange boundary, persistence, or OpenSpec workflow?
4. **Default tree** — Per [.cursor/context/refactor-ledger.md](../../context/refactor-ledger.md): prefer **TradingPlatform** for new work; legacy only when explicitly scoped.
5. **Bypass vs orchestrate** — If the user named a command (`/opsx:apply …`), a folder, or a skill, **bypass** extended routing and follow that contract.
6. **Stop condition** — If a single clarifying question removes ambiguity, ask it; otherwise state the plan in 3–7 bullets and proceed (or point to `/opsx:explore` for deep design-only mode).

## After preflight

- Load [.cursor/context/routing-map.md](../../context/routing-map.md) for paired-change hints and OpenSpec command selection.
- Execute in the **smallest** specialist surface: path-scoped rules, layer `AGENTS.md`, or the appropriate `openspec` command.
