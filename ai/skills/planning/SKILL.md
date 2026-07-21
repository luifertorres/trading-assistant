---
name: planning
description: Plan Mode and CreatePlan — produce granular, execution-ready todos for downstream agents.
triggers: [plan mode, create plan, planning, implementation plan, breakdown tasks]
---

# SKILL: Planning (Plan Mode / CreatePlan)

> Use when **Plan Mode** is active, when calling **CreatePlan**, or when the user asks for an implementation plan before coding.
>
> Downstream implementation agents should execute todos **without** re-searching the codebase for file locations or layer order.
>
> Granular todos apply to **all** buckets (Platform, legacy, MVP). **TDD todo pairing is conditional:** use it only when the target solution/project **already has any tests**; otherwise plan without test-first steps (e.g. legacy TradingAssistant).

## Mandatory output

Every plan MUST include a structured `todos` array in plan frontmatter (YAML `id` + `content` per item).

- Do **not** finish with prose-only task lists and no structured todos.
- Do **not** approve a plan whose todos still require discovery work (vague class names, missing paths).

## When to use TDD steps in todos

Before emitting todos, decide whether the **target** solution or project already has tests:

| Signal | Treat as |
|--------|----------|
| Matching `*Tests*.csproj` / `*.Tests/` under the same solution or sibling test project for the code you will change | **Has tests** → TDD pairing required for behavior changes |
| Platform / MVP solutions listed in [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md) | **Has tests** |
| Legacy TradingAssistant single project with no test csproj in scope | **No tests** → no `*-test-red` / `*-impl` pairing |

Do **not** invent a new test project in the plan just to enable TDD. Only pair when tests already exist.

### Has tests — behavior slices

For each **behavior** change (Domain, Application use cases, deterministic mappers/helpers, or equivalent testable logic):

1. Emit **`{name}-test-red`** then **`{name}-impl`** (never reverse).
2. `*-test-red` content names the test file/class, filtered `dotnet test` command, and expected failure.
3. Follow [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md) for the red/green meaning of those todos.

Host DI, composition roots, and pure wiring may stay single todos after the related behavior slice is green.

### No tests — all slices

Use ordinary path-specific todos (no test-red pairing). End with build (and any documented smoke/run command).

## Granularity rules

Each todo = **one verifiable deliverable**. The `content` field must satisfy **all** of:

1. **Repo-relative path** — at least one full path from repo root (e.g. `src/platform/TradingPlatform/src/MarketData/MarketData.Application/...`).
2. **Imperative action + artifact** — verb + type/method/migration name when known.
3. **Concrete scope** — what to add/change/remove; avoid "update service" or "add tests" alone.
4. **Template reference** — when copying an existing pattern, name the template file.

### Todo `id`

Short kebab-case, unique within the plan.

- With tests (behavior): `orchestrator-test-red`, `orchestrator-impl`, `verify`
- Without tests / non-behavior: `trade-request-fields`, `rsi5-extreme-1m`, `di-registration`, `verify`

### Layer / dependency order

Order todos so each step can compile and run after the previous one.

**Has tests (per behavior slice):** `test-red` → minimal types so the test compiles if needed → `impl` → optional refactor → Infrastructure → Host/Cli → **verify**.

**No tests:** types → core logic → adapters/persistence → Host/Cli → **verify**.

Split any todo that mentions "and" across two files or layers. Do **not** place `*-impl` before matching `*-test-red` when TDD pairing applies.

### Verify todo (required last step)

Always end with a single `verify` todo naming **exact** commands for the bucket:

1. `dotnet build` on the affected project or solution (always).
2. When tests exist for the change: `dotnet test` on the affected **unit** test csproj.
3. When Infrastructure persistence, Binance adapter, or store behavior changed on Platform: `dotnet test` on `MarketData.Infrastructure.IntegrationTests` csproj **last**.
4. Buckets without tests: build (and any documented run/smoke command) only — do not invent a test project.

Examples:

- Platform Application: build → `MarketData.Application.Tests.csproj` → integration csproj only if infra changed
- MVP: build `Backtesting.sln` → `Backtesting.Mvp.Tests.csproj`; or `WebSocketTrading.slnx` → `WebSocketTrading.Tests.csproj`
- Legacy TradingAssistant: `dotnet build src/legacy/TradingAssistant/TradingAssistant/TradingAssistant.csproj`

Never use only "run tests" or "build project" without paths.

### Count heuristic

| Scope | Typical todo count |
|-------|-------------------|
| Non-trivial Platform feature (with TDD pairs) | 8–15 |
| Small fix or single-file change | 3–5 |
| Legacy maintenance (no tests) | 3–8 |

Still path-specific even for small fixes.

## OpenSpec integration

| Mode | Planner output |
|------|----------------|
| Plan Mode / CreatePlan (no OpenSpec change) | Full YAML `todos` per this skill |
| `/opsx:apply` | Follow OpenSpec `tasks.md`; when the change targets a tested project, embed `*-test-red` before `*-impl` for behavior |
| `/opsx:explore` | No implementation todos |

When OpenSpec CLI is unavailable, read `openspec/changes/<name>/tasks.md` directly — see `openspec/SETUP.md`.

## Plan body (overview + sections)

- **overview**: one paragraph — what, which bucket/context, key contracts.
- Link constraints once — do not repeat [`AGENTS.md`](../../../AGENTS.md) in every todo.
- Use mermaid only for non-obvious flows; keep the plan concise.

## Examples

### Good — has tests (Platform behavior)

```yaml
- id: orchestrator-test-red
  content: In `src/platform/TradingPlatform/tests/MarketData.Application.Tests/Usdm1dBackfillOrchestratorTests.cs`, add `RunAsync_WhenEmptyRegistry_CompletesWithoutError` with fakes; run `dotnet test ... --filter FullyQualifiedName~Usdm1dBackfillOrchestratorTests` and confirm failure.
- id: orchestrator-impl
  content: In `src/platform/TradingPlatform/src/MarketData/MarketData.Application/Usdm1dBackfillOrchestrator.cs`, implement minimal logic so that test passes.
- id: verify
  content: Run `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx`; `dotnet test src/platform/TradingPlatform/tests/MarketData.Application.Tests/MarketData.Application.Tests.csproj`.
```

### Good — no tests (legacy)

```yaml
- id: trade-request-fields
  content: Restore optional fields on `src/legacy/TradingAssistant/TradingAssistant/TradeRequest.cs` (`MarginPercentage`, `StopLossPrice`, `TakeProfitPrice`, …) from git template `9f82028`.
- id: rsi5-extreme-1m
  content: Add `src/legacy/TradingAssistant/TradingAssistant/Rsi5Extreme1mStrategy.cs` (1m RSI(5) cross-up; template `Rsi5ExtremeStrategy` from git `9f82028`).
- id: verify
  content: Run `dotnet build src/legacy/TradingAssistant/TradingAssistant/TradingAssistant.csproj`.
```

### Bad (anti-patterns)

| Bad | Fix |
|-----|-----|
| `*-impl` before `*-test-red` on a tested project | Flip order |
| TDD pairing on legacy with no test csproj | Drop test-red; path-specific impl + build verify |
| "Update TradeHandler" | Name the file path and the concrete gate or branch to add |
| "Run all tests" | Name exact build (and test csproj only when it exists) |

## Before finalizing the plan

1. Research enough to name real paths; decide **has tests / no tests** for the target.
2. Read each todo as an implementation agent: can they execute without broad search?
3. Split any todo that mentions "and" across two files or layers.
4. If has tests: confirm every behavior slice has `*-test-red` before `*-impl`.
5. Confirm final todo is `verify` with concrete `dotnet` (or documented smoke) commands for that bucket.

## Related routing

- Strict TDD (implementation): [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md)
- Verify order: [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md)
- Editor enforcement: run `ai/commands/ai-onboard.md` after changing `ai/templates/rules/` or this skill.
- Root index: [`AGENTS.md`](../../../AGENTS.md)
