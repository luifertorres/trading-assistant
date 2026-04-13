# TradingPlatform — Agent Instructions

## Purpose

**Greenfield** modular monolith for trading: bounded contexts with **Domain / Application / Infrastructure** slices, plus **TradingPlatform.Kernel** building blocks and **delivery** apps (Host, Cli).

This tree is the **intended future primary** codebase. It must **not** reference `TradingAssistant`, `CandlestickData`, or `Backtesting` projects.

## Entry points

- Run and build: [README.md](./README.md)
- Solution: [TradingPlatform.slnx](./TradingPlatform.slnx)
- Ubiquitous language and context map: [docs/GLOSSARY.md](./docs/GLOSSARY.md), [docs/ADRs.md](./docs/ADRs.md)
- DDD workbook: [docs/design-journey/00-how-to-use-this-trail.md](./docs/design-journey/00-how-to-use-this-trail.md)

## Bounded contexts (physical)


| Context        | Projects                                                          |
| -------------- | ----------------------------------------------------------------- |
| BuildingBlocks | `src/BuildingBlocks/TradingPlatform.Kernel`                       |
| MarketData     | `src/MarketData/MarketData.{Domain,Application,Infrastructure}`   |
| Research       | `src/Research/Research.{Domain,Application,Infrastructure}`       |
| Analytics      | `src/Analytics/Analytics.{Domain,Application,Infrastructure}`     |
| Portfolio      | `src/Portfolio/Portfolio.{Domain,Application,Infrastructure}`     |
| Execution      | `src/Execution/Execution.{Domain,Application,Infrastructure}`     |
| Delivery       | `src/Hosts/TradingPlatform.Host`, `src/Tools/TradingPlatform.Cli` |


Cross-context dependencies exist (example: Execution Application references Research Application and Portfolio Domain). **Prefer** explicit project references and anti-corruption at boundaries over shared “god” models. When in doubt, align with [docs/GLOSSARY.md](./docs/GLOSSARY.md) and the context map in the design journey.

## Dependency discipline

- **Domain** projects: pure policy and domain types; avoid broker and IO frameworks in inner models (broker ACL lives at infrastructure edges).
- **Application**: orchestration, use cases, ports; depend inward on Domain (and allowed cross-context contracts per project references).
- **Infrastructure**: adapters, persistence, external APIs.
- **Host / Cli**: composition roots, configuration, process lifetime.

## OpenSpec (repo-wide)

Same OpenSpec layout as the repository root:

- Main specs: `openspec/specs/<capability>/spec.md`
- Changes: `openspec/changes/<change-name>/`

Use `/opsx:apply <change-name>` (or other `opsx` commands) when doing spec-driven work that applies to Platform features. Platform-specific deltas should still live under `openspec/changes/…` unless you are updating main specs intentionally (`/opsx:sync`).

## Repo-wide agent context

For **routing** (Platform vs legacy), slash-command bypasses, and paired-change hints, read:

- [.cursor/context/routing-map.md](../../.cursor/context/routing-map.md)
- [.cursor/context/refactor-ledger.md](../../.cursor/context/refactor-ledger.md)