# Context deep dive — Execution

## Learning objective

Clarify the **live path**: translate portfolio + active vector + bars into **order intents** via the same strategy abstraction as research, while isolating the **broker** behind an ACL and a live sink.

## Strategic recap

- **Subdomain:** core for real trading; generic if only routing stubs exist.
- **Upstream/downstream:** downstream of Portfolio (`PortfolioDefinition`) and Research (`ITradingStrategyFactory`); upstream to external broker (stub today).

## Prerequisites

- [10-context-portfolio.md](./10-context-portfolio.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | One-shot replay over historical bars for active vector | Intents flushed to `ILiveOrderIntentSink` |
| 2 | Active vector not in portfolio | `InvalidOperationException` (current behavior) |
| 3 | Unknown `TradingVectorId` in map | `InvalidOperationException` |
| 4 | Live feed (future) | Define long-running process vs one-shot |

## Model sketch

- **Router:** `PortfolioExecutionRouter` — orchestrates strategy + adapter.
- **ACL:** `BrokerAntiCorruptionStub` — placeholder for exchange-specific translation ([ADRs.md](../ADRs.md) **ADR-004**).
- **Sink:** `LoggingLiveOrderIntentSink` — MVP I/O.

## Application ports

| Port | Responsibility |
|------|------------------|
| `ILiveOrderIntentSink` | Accept intents for broker path — [ILiveOrderIntentSink.cs](../../src/Execution/Execution.Application/ILiveOrderIntentSink.cs) |
| `PortfolioExecutionRouter` | Compose strategy + adapt simulation sink interface to live — [PortfolioExecutionRouter.cs](../../src/Execution/Execution.Application/PortfolioExecutionRouter.cs) |

## Infrastructure choices

- Stub broker + logging sink + DI: [Execution.Infrastructure](../../src/Execution/Execution.Infrastructure/) (`BrokerAntiCorruptionStub.cs`, `LoggingLiveOrderIntentSink.cs`, `ServiceCollectionExtensions.cs`).
- `Execution.Domain` — [ExecutionModes.cs](../../src/Execution/Execution.Domain/ExecutionModes.cs) (extend as modes grow).

## Compare with repo

| Artifact | Path |
|----------|------|
| Router | [PortfolioExecutionRouter.cs](../../src/Execution/Execution.Application/PortfolioExecutionRouter.cs) |
| Live sink | [LoggingLiveOrderIntentSink.cs](../../src/Execution/Execution.Infrastructure/LoggingLiveOrderIntentSink.cs) |
| ACL stub | [BrokerAntiCorruptionStub.cs](../../src/Execution/Execution.Infrastructure/BrokerAntiCorruptionStub.cs) |
| **ADR-003** adapter | Inner type `LiveStrategySinkAdapter` in router implements `ISimulationOrderIntentSink` |

## Open questions / ADR candidates

- When Binance (or other) arrives, split **OHS** for market data vs **ACL** for orders?
- Where does **position / order state** live long term—Execution domain vs new context?

## Next doc

[12-delivery-host-and-cli.md](./12-delivery-host-and-cli.md)
