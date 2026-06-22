---
name: dotnet-verification
description: Writing and running unit and integration tests. xUnit + FluentAssertions; fakes or NSubstitute.
triggers: [write tests, unit test, integration test, coverage, FluentAssertions, dotnet test, verify]
---

# SKILL: .NET verification

> Packages: xunit, FluentAssertions, coverlet; NSubstitute where already referenced.
>
> **Strict TDD (test first):** [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md) — failing unit test before production behavior changes.

## TDD quick loop (unit tests)

1. **Red** — Add `Method_Scenario_ExpectedResult` in the matching test class; run `dotnet test <csproj> --filter FullyQualifiedName~{TestClass}`; confirm failure.
2. **Green** — Minimal production change until the test passes.
3. **Refactor** — Keep the test project green.

Example red test (Domain — pure, no mocks):

```csharp
[Theory]
[InlineData("1d")]
[InlineData("1D")]
public void Parse_AcceptsDailyAliases(string raw)
{
    TimeFrameCode.Parse(raw).Should().Be(TimeFrameCode.Day1);
}
```

Example red test (Application — hand-rolled fakes):

```csharp
[Fact]
public async Task RunAsync_WhenOneInstrumentFails_ContinuesAndRecordsFailure()
{
    var exchange = new FakeExchange(/* ... */);
    exchange.OnGetDailyKlinesPageAsync = handle => Task.FromException<IReadOnlyList<OhlcBar>>(new InvalidOperationException("synthetic failure"));
    var orchestrator = CreateOrchestrator(new RecordingWriter(), exchange, /* ... */);
    await orchestrator.RunAsync(Options());
    // assert failure recorded, healthy instrument still processed
}
```

Run red, implement, run green. Full templates below.

## Test projects (this repo)

| Bucket | Kind | Path |
|--------|------|------|
| Platform — MarketData Domain | Unit | `src/platform/TradingPlatform/tests/MarketData.Domain.Tests/` |
| Platform — MarketData Application | Unit | `src/platform/TradingPlatform/tests/MarketData.Application.Tests/` |
| Platform — Cli | Unit | `src/platform/TradingPlatform/tests/TradingPlatform.Cli.Tests/` |
| Platform — MarketData Infrastructure | Integration | `src/platform/TradingPlatform/tests/MarketData.Infrastructure.IntegrationTests/` |
| MVP Backtesting | Unit | `src/mvp/Backtesting/Backtesting.Mvp.Tests/` |
| Legacy | Unit (maintenance) | `src/legacy/TradingAssistant/CandlestickData.Tests/` if present |

Run all Platform tests: `dotnet test src/platform/TradingPlatform/TradingPlatform.slnx`. Module-scoped: `dotnet test <path-to-csproj>`.

## Verify order (Definition of Done)

1. `dotnet build src/platform/TradingPlatform/TradingPlatform.slnx` (or `Backtesting.sln` for MVP)
2. `dotnet test` on affected **unit** csproj(s) with `--filter` when possible
3. **Last** — when Infrastructure persistence, Binance adapter, SQLite store, or instrument registry behavior changed: `dotnet test src/platform/TradingPlatform/tests/MarketData.Infrastructure.IntegrationTests/MarketData.Infrastructure.IntegrationTests.csproj`

**Integration required in DoD when:** new/changed persistence adapter, Binance backfill client wiring, or store/registry integration behavior.

**Unit verify only when:** Domain/Application/Cli-only work with no Infrastructure surface change.

**Live Binance tests:** classes like `BinanceUsdM1dBackfillExchangeLiveTests` require network and credentials. **Never** required for ordinary PR verify. Opt-in:

```bash
dotnet test .../MarketData.Infrastructure.IntegrationTests.csproj --filter "FullyQualifiedName~Sqlite"
```

## Testing patterns by layer

**Domain tests (pure, no mocks):**

```csharp
public sealed class TimeFrameCodeDay1Tests
{
    [Fact]
    public void Day1_Value_Is1D()
    {
        TimeFrameCode.Day1.Value.Should().Be("1D");
    }
}
```

**Application tests (fakes preferred on Platform):**

- Co-locate `FakeExchange`, `RecordingWriter`, `InMemoryCheckpointStore` in the test file or a `Fakes/` folder.
- NSubstitute allowed where the project already references it.

**Infrastructure integration tests:**

- SQLite in-memory or temp files for store/registry tests.
- Live Binance tests gated; run last and only when explicitly validating exchange integration.

## FluentAssertions essentials

```csharp
result.Should().NotBeNull();
result.Should().Be(expected);
list.Should().HaveCount(3);
act.Should().ThrowAsync<InvalidOperationException>();
```

## Naming: `Method_Scenario_ExpectedResult`

## Coverage

No hard coverage gate today. Prefer meaningful tests on Domain and Application behavior over line-count targets.

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

## MVP verify

```bash
dotnet build src/mvp/Backtesting/Backtesting.sln
dotnet test src/mvp/Backtesting/Backtesting.Mvp.Tests/Backtesting.Mvp.Tests.csproj
```

## Related routing

- Strict TDD: [`ai/skills/test-driven-development/SKILL.md`](../test-driven-development/SKILL.md)
- Root index: [`AGENTS.md`](../../../AGENTS.md)
