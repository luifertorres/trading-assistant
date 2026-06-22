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

## OpenSpec (Platform primary)

Read the two-track map: [openspec/README.md](../../../openspec/README.md).

- **Canonical Platform specs:** `openspec/specs/trading-platform-*/spec.md` (MarketData today; Research simulation as it lands).
- **Active changes:** `openspec/changes/<name>/` — use names prefixed `trading-platform-…` for new work.
- **Legacy specs** (`architecture`, `trading-strategies`, etc.) document `src/legacy/TradingAssistant/` only; do not extend them for Platform features.

Use `/opsx:new` to start Platform changes, then `/opsx:apply`, `/opsx:sync`, `/opsx:archive` as usual. Archived legacy proposals live under `openspec/changes/archive/` with `LEGACY.md`. Setup: [`openspec/SETUP.md`](../../../openspec/SETUP.md).

## Test projects

| Kind | Path |
|------|------|
| MarketData Domain (unit) | `tests/MarketData.Domain.Tests/MarketData.Domain.Tests.csproj` |
| MarketData Application (unit) | `tests/MarketData.Application.Tests/MarketData.Application.Tests.csproj` |
| Cli (unit) | `tests/TradingPlatform.Cli.Tests/TradingPlatform.Cli.Tests.csproj` |
| MarketData Infrastructure (integration) | `tests/MarketData.Infrastructure.IntegrationTests/MarketData.Infrastructure.IntegrationTests.csproj` |

Full suite: `dotnet test TradingPlatform.slnx` from this directory. Patterns and verify order: [`ai/skills/dotnet-verification/SKILL.md`](../../../ai/skills/dotnet-verification/SKILL.md). Strict TDD: [`ai/skills/test-driven-development/SKILL.md`](../../../ai/skills/test-driven-development/SKILL.md).

**Verify order:** `dotnet build TradingPlatform.slnx` → affected unit csproj → `MarketData.Infrastructure.IntegrationTests` **last** when persistence/Binance adapter changed.

## Repo-wide agent context

For **routing** (Platform vs legacy), slash-command bypasses, and paired-change hints, read:

- [ai/context/routing-map.md](../../../ai/context/routing-map.md)
- [ai/context/refactor-ledger.md](../../../ai/context/refactor-ledger.md)