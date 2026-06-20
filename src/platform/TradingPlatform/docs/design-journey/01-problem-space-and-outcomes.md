# Problem space and outcomes

## Learning objective

Separate **what the business is trying to achieve** (outcomes, jobs-to-be-done) from **how the code is structured**, so later bounded contexts trace back to real needs.

## Prerequisites

- [00-how-to-use-this-trail.md](./00-how-to-use-this-trail.md)

## Workshop: outcomes and journeys

### 1. Stakeholder outcomes (fill)

| Outcome | Success signal | Non-goals (explicit) |
|--------|------------------|----------------------|
| (e.g. compare strategy variants) | | |
| (e.g. deploy a portfolio to live) | | |

### 2. User / operator journeys (not classes)

Draw 2–4 journeys as bullet timelines: trigger → steps → done.

**Suggested seeds** (adapt to your voice):

- **Research:** configure a trading vector → run simulation → inspect equity / drawdown / trades.
- **Selection:** compare multiple runs → rank or filter → explain correlation between runs.
- **Portfolio:** compose a set of vectors with weights → persist definition → hand off to execution.
- **Execution:** activate one vector from a portfolio → stream bars → emit order intents to broker path.

### 3. Map journeys to the CLI demo

The greenfield demo composes a vertical slice in one command. Map **each journey step** to a line or block in code.

**Repo anchor:** [TradingPlatform.Cli/Program.cs](../../src/Tools/TradingPlatform.Cli/Program.cs) (`RunDemoAsync`).

| Your journey step | Demo code step (describe) | Approx. lines (update if file moves) |
|--------------------|---------------------------|--------------------------------------|
| Persist candles | `ICandleSeriesWriter.UpsertAsync` | ~61–64 |
| Run backtests | `IBacktestRunner.RunAsync` ×2 | ~66–83 |
| Persist runs | `ISimulationRunRepository.SaveAsync` | ~84–85 |
| Rank | `IRunAnalytics.RankByReturn` | ~88–89 |
| Compose portfolio | `IPortfolioComposer.ComposeDrawdownUncorrelated` + save | ~91–93 |
| Route execution | `PortfolioExecutionRouter.ExecuteOneShotAsync` | ~95–98 |

## Compare with repo

- [README.md](../README.md) — `dotnet run … -- demo` documents the same slice.
- [ADRs.md](../ADRs.md) — strategic packaging (modular monolith) reflects “solo operator, one deployable” outcome.

## Open questions / ADR candidates

- Who is the “user”: you as quant, a separate operator, or an automated scheduler?
- Paper vs live: same outcomes, different risk—does that split a context later?

## Next doc

[02-event-storming-big-picture.md](./02-event-storming-big-picture.md)
