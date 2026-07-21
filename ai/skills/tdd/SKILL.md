---
name: tdd
description: Strict test-driven development — failing unit test before production code; Red-Green-Refactor.
triggers: [TDD, tdd, test first, red green refactor, strict TDD, write test before, test-driven]
---

# SKILL: Strict TDD (unit tests)

> **Hard mode:** Do not add or change Domain behavior, Application use cases, or deterministic mapper logic until a **failing** unit test exists in the matching test project.
>
> Specs, OpenSpec tasks, and plans define **what** to build; TDD defines **proof** of done — whether or not you used SDD/planning first.
>
> Test patterns and verify order: [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md). For editor-side enforcement rules, run `ai/commands/ai-onboard.md` when `ai/templates/rules/` or `ai/commands/` changes.

## When this skill applies

- New or changed behavior in **Domain**, **Application** use cases, or **deterministic mappers/helpers**.
- Bug fixes that change observable domain/application behavior.
- Platform greenfield work under `src/platform/TradingPlatform/`.

## When to use a variant (still require tests)

| Change type | TDD approach |
|-------------|--------------|
| New public method on existing service/handler | Red unit test first; add interface signature only if the test needs it |
| New domain type + behavior | Red test in Domain.Tests; minimal types allowed so the test compiles |
| EF migration / persistence shape | Contract or mapping unit test for **JSON/entity shape** before migration when behavior is data-shaped |
| Host / DI / Cli wiring | **After** unit tests are green; keep composition roots thin |
| Rename/refactor only | Update tests first (or in the same step); suite must stay green |

## Out of scope (red/green loop)

- Integration / live Binance tests — add after unit tests are green; see [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md).

## Test projects (this repo)

| Bucket | Path | When |
|--------|------|------|
| Platform — MarketData Domain | `src/platform/TradingPlatform/tests/MarketData.Domain.Tests/` | Domain + Kernel pure logic |
| Platform — MarketData Application | `src/platform/TradingPlatform/tests/MarketData.Application.Tests/` | Handlers, orchestrators, use cases |
| Platform — Cli | `src/platform/TradingPlatform/tests/TradingPlatform.Cli.Tests/` | CLI parsing/smoke |
| Platform — Infrastructure | `src/platform/TradingPlatform/tests/MarketData.Infrastructure.IntegrationTests/` | **After** unit green only |
| MVP Backtesting | `src/mvp/Backtesting/Backtesting.Mvp.Tests/` | Backtesting MVP changes |
| MVP WebSocketTrading | `src/mvp/WebSocketTrading/WebSocketTrading.Tests/` | WebSocketTrading MVP changes |
| Legacy | `src/legacy/TradingAssistant/` | Maintenance only; no new feature TDD unless explicit bugfix |

Pick the project that references the layer you are changing.

## Red → Green → Refactor

### 1. Red

1. Add or update a unit test: naming `Method_Scenario_ExpectedResult` (see dotnet-verification skill).
2. Use **hand-rolled fakes** (Platform convention) or NSubstitute where already used in the project.
3. Assert the **desired** behavior (not the current broken behavior).
4. Run from repo root:

```bash
dotnet test <path-to-test.csproj> --filter "FullyQualifiedName~YourTestClassName"
```

5. Confirm **failure for the right reason**: missing implementation, wrong return value — not unrelated compile errors.

### 2. Green

1. Change **minimal** production code to pass **only** that test.
2. Re-run the same filtered `dotnet test` until the new test passes.
3. Run the full test project before moving on.

### 3. Refactor

1. Improve structure without changing behavior.
2. Keep all tests green; re-run `dotnet test` on the test project.

## Definition of Done (per behavior slice)

- [ ] Failing unit test existed before production behavior change (red confirmed).
- [ ] Minimal implementation makes the test pass (green).
- [ ] `dotnet test <unit-test-csproj>` passes for the affected project.
- [ ] `dotnet build` for the solution still passes.

Per-slice DoD is **unit-only**. Integration tests are not used for red/green.

## Definition of Done (PR / change completion)

After all unit slices are green:

- [ ] `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx` (or affected solution)
- [ ] `dotnet test` on affected unit csproj(s)
- [ ] When Infrastructure persistence, Binance adapter, or store behavior changed: `dotnet test` on `MarketData.Infrastructure.IntegrationTests` csproj **last**

Verify order: [`ai/skills/dotnet-verification/SKILL.md`](../dotnet-verification/SKILL.md).

## Plan Mode / CreatePlan

When the target already has tests, planning emits **`*-test-red`** then **`*-impl`** — see [`ai/skills/planning/SKILL.md`](../planning/SKILL.md). This skill defines the red/green meaning of those todos.

Never place `*-impl` before the matching `*-test-red` todo when TDD pairing applies.

## OpenSpec coexistence

When implementing via `/opsx:apply`, OpenSpec `tasks.md` defines *what*; each behavior task on a tested project still splits into `*-test-red` then `*-impl`.

## Anti-patterns

- Implementing production behavior before creating the unit test file.
- "Add tests at the end" or a single coarse "add tests" todo without a prior red step.
- Skipping red confirmation (assuming the test would fail without running `dotnet test`).
- Broad `dotnet test` on the whole solution as the only red check when a filtered run is possible.
- Putting business assertions only in integration tests when the logic lives in Domain/Application.

## Related routing

- Planning todos (TDD pairing when target already has tests): [`ai/skills/planning/SKILL.md`](../planning/SKILL.md)
- Explicit workflow command: [`ai/commands/tdd.md`](../../commands/tdd.md)
- Root index: [`AGENTS.md`](../../../AGENTS.md)
