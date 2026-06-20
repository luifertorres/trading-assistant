# Context deep dive — Portfolio

## Learning objective

Define **portfolio** as a versioned, named allocation over **trading vectors**—a **published language** toward Execution—and separate **composition policy** from **storage**.

## Strategic recap

- **Subdomain:** core if portfolio construction is your edge; supporting if it is a thin weighting layer.
- **Upstream/downstream:** downstream of Research (full `SimulationRunResult`) and Analytics (`IRunAnalytics`); upstream to Execution (`PortfolioDefinition`).

## Prerequisites

- [09-context-analytics.md](./09-context-analytics.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | Compose “drawdown-uncorrelated” set from runs | `PortfolioDefinition` with equal weights over picked vectors |
| 2 | Save portfolio to disk | JSON (or other) durable artifact |
| 3 | Empty run list | Current composer returns empty members — confirm desired UX |
| 4 | Rebalance / version bump | (define: new `PortfolioId` vs same id new `AsOf`) |

## Model sketch

- **Aggregate candidate:** **Portfolio** (identity `PortfolioId`, invariant: weights sum to 1, members reference known `TradingVectorId`).
- **Composition policy:** `IPortfolioComposer` implementation uses analytics matrix — see [PortfolioComposer.cs](../../src/Portfolio/Portfolio.Infrastructure/PortfolioComposer.cs).

## Application ports

| Port | Responsibility |
|------|------------------|
| `IPortfolioComposer` | Build `PortfolioDefinition` from inputs — [IPortfolioComposer.cs](../../src/Portfolio/Portfolio.Application/IPortfolioComposer.cs) |

## Infrastructure choices

- File repository for definitions: [FilePortfolioRepository.cs](../../src/Portfolio/Portfolio.Infrastructure/FilePortfolioRepository.cs).
- DI: [Portfolio.Infrastructure/ServiceCollectionExtensions.cs](../../src/Portfolio/Portfolio.Infrastructure/ServiceCollectionExtensions.cs).

## Compare with repo

| Artifact | Path |
|----------|------|
| Domain model | [PortfolioDefinition.cs](../../src/Portfolio/Portfolio.Domain/PortfolioDefinition.cs), `PortfolioMember` in same file |
| Composer | [PortfolioComposer.cs](../../src/Portfolio/Portfolio.Infrastructure/PortfolioComposer.cs) |
| CLI wiring | [TradingPlatform.Cli/Program.cs](../../src/Tools/TradingPlatform.Cli/Program.cs) (`FilePortfolioRepository`, `ComposeDrawdownUncorrelated`) |

## Open questions / ADR candidates

- Move composition **policy** to Domain service vs keep in Infrastructure (current).
- Published language: does Execution need **only** `PortfolioDefinition`, or also **run provenance** (audit)?

## Next doc

[11-context-execution.md](./11-context-execution.md)
