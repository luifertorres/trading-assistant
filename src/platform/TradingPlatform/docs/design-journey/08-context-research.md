# Context deep dive — Research

## Learning objective

Separate **simulation** (deterministic evaluation of a strategy over bars) from **persistence of runs** and from **live execution**, while reusing the same strategy boundary where intentional.

## Strategic recap

- **Subdomain:** typically **core** (your alpha lives here)—unless you treat it as lab tooling; decide explicitly.
- **Upstream/downstream:** downstream of MarketData (bars); upstream to Analytics and Portfolio (run results); collaborates with Execution via shared strategy factory pattern ([ADRs.md](../ADRs.md) **ADR-003**).

## Prerequisites

- [07-context-market-data.md](./07-context-market-data.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | Run backtest for `TradingVector` + `SimulationConfiguration` | `SimulationRunResult` with trades, equity, max drawdown |
| 2 | Strategy emits intents on bar N | Simulation sink records fills / virtual PnL per your sink rules |
| 3 | Same vector, two runs | Distinct `RunId`, comparable metrics |
| 4 | Invalid strategy parameters | (define: fail fast vs degraded run) |

## Model sketch

- **Key records:** `SimulationConfiguration`, `SimulationRunResult`, `TradeRecord`, `EquityPoint` — [SimulationModels.cs](../../src/Research/Research.Domain/SimulationModels.cs).
- **Aggregate candidate:** **SimulationRun** (lifecycle: requested → executing → completed with immutable result).
- **Strategy plug-in:** `ITradingStrategy` + `BarProcessingContext` + sink abstraction.

## Application ports

| Port | Responsibility |
|------|----------------|
| `IBacktestRunner` | Orchestrate load bars (when implemented) / iterate strategy / produce `SimulationRunResult` |
| `ITradingStrategyFactory` | Resolve strategy by kind + parameters |
| `ISimulationRunRepository` | Persist / load run results |
| `ISimulationOrderIntentSink` | Accept intents during simulation (vs live sink) — see [IOrderIntentSink.cs](../../src/Research/Research.Application/IOrderIntentSink.cs) |

## Infrastructure choices

- SQLite persistence for runs: [SqliteSimulationRunRepository.cs](../../src/Research/Research.Infrastructure/SqliteSimulationRunRepository.cs).
- Reference strategy: [FixedWindowStrategy.cs](../../src/Research/Research.Infrastructure/FixedWindowStrategy.cs).
- Simulation sink: [SimulationOrderIntentSink.cs](../../src/Research/Research.Infrastructure/SimulationOrderIntentSink.cs).

## Compare with repo

| Artifact | Path |
|----------|------|
| Runner (uses `ICandleSeriesReader` from MarketData) | [BacktestRunner.cs](../../src/Research/Research.Infrastructure/BacktestRunner.cs), [IBacktestRunner.cs](../../src/Research/Research.Application/IBacktestRunner.cs) |
| Request DTO | `BacktestRequest` in [IBacktestRunner.cs](../../src/Research/Research.Application/IBacktestRunner.cs) |
| Strategy API | [ITradingStrategy.cs](../../src/Research/Research.Application/ITradingStrategy.cs), [BarProcessingContext.cs](../../src/Research/Research.Application/BarProcessingContext.cs) |
| Factory | [DefaultTradingStrategyFactory.cs](../../src/Research/Research.Infrastructure/DefaultTradingStrategyFactory.cs), [ITradingStrategyFactory.cs](../../src/Research/Research.Application/ITradingStrategyFactory.cs) |
| DI | [Research.Infrastructure/ServiceCollectionExtensions.cs](../../src/Research/Research.Infrastructure/ServiceCollectionExtensions.cs) |

## Open questions / ADR candidates

- **ADR-003:** extend with rules for partial fills, slippage model, or funding (if perpetuals).
- Should `BacktestRequest` **From/To** always load from MarketData reader inside runner (current direction) vs caller-supplied bars only?

## Next doc

[09-context-analytics.md](./09-context-analytics.md)
