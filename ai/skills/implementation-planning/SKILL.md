---
name: implementation-planning
description: Plan Mode and CreatePlan — produce granular, execution-ready todos for downstream agents.
triggers: [plan mode, create plan, planning, implementation plan, breakdown tasks]
---

# SKILL: Implementation planning (Plan Mode / CreatePlan)

> Use when **Plan Mode** is active, when calling **CreatePlan**, or when the user asks for an implementation plan before coding.
>
> Downstream implementation agents should execute todos **without** re-searching the codebase for file locations or layer order.

## Mandatory output

Every plan MUST include a structured `todos` array in plan frontmatter (YAML `id` + `content` per item).

- Do **not** finish with prose-only task lists and no structured todos.
- Do **not** approve a plan whose todos still require discovery work (vague class names, missing paths).

## Granularity rules

Each todo = **one verifiable deliverable**. The `content` field must satisfy **all** of:

1. **Repo-relative path** — at least one full path from repo root (e.g. `src/platform/TradingPlatform/src/MarketData/MarketData.Application/...`).
2. **Imperative action + artifact** — verb + type/method/migration name when known.
3. **Concrete scope** — what to add/change/remove; avoid "update service" or "add tests" alone.
4. **Template reference** — when copying an existing pattern, name the template file.

### Todo `id`

Short kebab-case, unique within the plan: `orchestrator-test-red`, `orchestrator-impl`, `di-registration`, `verify`.

For behavior changes, use **`{slice}-test-red`** then **`{slice}-impl`** (see [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md)).

### Layer order (Platform bounded context work)

**Per behavior slice (strict TDD):**

`test-red (unit)` → interfaces/DTOs/domain skeleton if needed for compile → `impl (green)` → optional refactor → Infrastructure (adapters, persistence) → Host/Cli (composition) → **verify**.

Do **not** schedule Application `*-impl` before matching `*-test-red`.

### Verify todo (required last step)

Always end with a single `verify` todo naming **exact** commands in **order**:

1. `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx` from repo root
2. `dotnet test` on the affected **unit** test csproj
3. When Infrastructure persistence, Binance adapter, or store behavior changed: `dotnet test` on `MarketData.Infrastructure.IntegrationTests` csproj **last**

Examples:

- MarketData Application work: build → `MarketData.Application.Tests.csproj` → `MarketData.Infrastructure.IntegrationTests.csproj` only if infra changed
- Domain-only: build → `MarketData.Domain.Tests.csproj`
- MVP: build `Backtesting.sln` → `Backtesting.Mvp.Tests.csproj`

Scope gate: [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md). Never use only "run tests" or "build project".

### Count heuristic

| Scope | Typical todo count |
|-------|-------------------|
| Non-trivial Platform feature | 8–15 |
| Small fix or single-file change | 3–5 |

Still path-specific even for small fixes.

## OpenSpec integration

| Mode | Planner output |
|------|----------------|
| Plan Mode / CreatePlan (no OpenSpec change) | Full YAML `todos` per this skill |
| `/opsx:apply` | Follow OpenSpec `tasks.md`; embed TDD pairing (`*-test-red` before `*-impl`) inside each behavior task |
| `/opsx:explore` | No implementation todos |

When OpenSpec CLI is unavailable, read `openspec/changes/<name>/tasks.md` directly — see `openspec/SETUP.md`.

## Plan body (overview + sections)

- **overview**: one paragraph — what, which bucket/context, key contracts.
- Link constraints once — do not repeat [`AGENTS.md`](../../../AGENTS.md) in every todo.
- Use mermaid only for non-obvious flows; keep the plan concise.

## Examples

### Good (granular — TDD order)

```yaml
- id: orchestrator-test-red
  content: In `src/platform/TradingPlatform/tests/MarketData.Application.Tests/Usdm1dBackfillOrchestratorTests.cs`, add `RunAsync_WhenEmptyRegistry_CompletesWithoutError` with fakes; run `dotnet test src/platform/TradingPlatform/tests/MarketData.Application.Tests/MarketData.Application.Tests.csproj --filter FullyQualifiedName~Usdm1dBackfillOrchestratorTests` and confirm failure.
- id: orchestrator-impl
  content: In `src/platform/TradingPlatform/src/MarketData/MarketData.Application/Usdm1dBackfillOrchestrator.cs`, implement minimal logic so `RunAsync_WhenEmptyRegistry_CompletesWithoutError` passes.
- id: verify
  content: Run `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx`; `dotnet test src/platform/TradingPlatform/tests/MarketData.Application.Tests/MarketData.Application.Tests.csproj`.
```

### Bad (anti-patterns)

| Bad | Fix |
|-----|-----|
| `service-impl` before `service-test-red` | Add `service-test-red` with test path, filter, expected failure first |
| Single `add tests` after all production code | Split into `*-test-red` then `*-impl` per slice |
| "Run all tests" | Name exact csproj paths |

## Before finalizing the plan

1. Research enough to name real paths (grep/read context `AGENTS.md` and similar features).
2. Read each todo as an implementation agent: can they execute without broad search?
3. Split any todo that mentions "and" across two files or layers.
4. Confirm final todo is `verify` with concrete `dotnet` commands.

## Related routing

- Strict TDD: [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md)
- Editor enforcement: run `ai/commands/ai-onboard.md` after changing `ai/templates/rules/` or plan/TDD rules are missing locally.
- Root index: [`AGENTS.md`](../../../AGENTS.md)
