# Test-driven implementation (strict unit tests)

Use when the user wants to implement or change behavior using **strict TDD** (unit tests first). Canonical detail: `ai/skills/test-driven-development/SKILL.md`.

## Constraints

- Do **not** change Domain behavior, Application use cases, or deterministic mappers until a **failing** unit test exists in the correct test csproj for the bucket being changed (Platform, MVP, or legacy maintenance).
- Confirm **red** with filtered `dotnet test` before writing production behavior.
- Host DI, composition roots, and CLI wiring come **after** unit tests are green (unless the slice is data-contract-only).
- No secrets in test data or logs.
- Legacy monolith: maintenance-only; no new feature TDD unless explicit bugfix scope.

## Steps

1. Read `ai/skills/test-driven-development/SKILL.md` and `ai/skills/dotnet-verification/SKILL.md`; pick the test project for the bucket being changed.
2. **Red** — Add or update a unit test (`Method_Scenario_ExpectedResult`). Run `dotnet test <test.csproj> --filter "FullyQualifiedName~<TestClass>"` and confirm failure for the right reason.
3. **Green** — Implement minimal production code so only that test passes. Re-run the same filtered test, then the full test project.
4. **Refactor** — Improve structure without changing behavior; keep tests green.
5. Report: test name, red failure reason, green command output, and final `dotnet test <unit-test.csproj>` result. When Infrastructure persistence or Binance adapter changed: also `dotnet test` on `MarketData.Infrastructure.IntegrationTests` csproj **last** (see `ai/skills/dotnet-verification/SKILL.md`).
