# Application Layer - Agent Instructions

## Purpose

Orchestration layer between Domain and Infrastructure. Defines **interfaces** for infrastructure services, **MediatR notifications/requests** for in-process messaging, and will eventually contain all **handlers and strategies** (currently in Host, pending migration).

## Rules

### Dependency Constraints

- **MAY** reference `TradingAssistant.Domain`.
- **NEVER** reference `TradingAssistant.Infrastructure` or the Host project.
- **NEVER** add infrastructure NuGet packages (EF Core, Binance.Net, FASTER, etc.).
- **ALLOWED** packages: `MediatR.Contracts` (for `INotification`, `IRequest`), `Microsoft.Extensions.DependencyInjection.Abstractions`.

### Interfaces

- Define abstractions for all infrastructure concerns:
  - `IExchangeService` — broker operations (open/close positions, get account info).
  - `ICandleRepository` — candle data persistence and retrieval.
  - `ITradingSignalQueue` — in-process signal queue.
  - `IClock` — time abstraction for testability.
- Interfaces use **domain types** in their signatures (not infrastructure types like Binance DTOs).
- Return `Task` or `Task<T>` with `CancellationToken` parameters.

### MediatR Notifications & Requests

- `CandleClosedNotification` — published when a candle closes on any timeframe.
- `SmasAndRsisCalculatedEvent` — published after indicator calculation completes.
- `TradingSignalNotification` — a trading signal has been generated.
- `IndicatorConditionMetNotification` — an indicator threshold was crossed.
- Keep notification classes as simple DTOs (records preferred).

### DI Registration

- `ServiceCollectionExtensions.AddApplication()` registers MediatR scanning the Application assembly.
- The Host also registers MediatR for its own assembly (for handlers still in Host).

## Current Contents

| File | Type | Description |
|------|------|-------------|
| `IExchangeService.cs` | Interface | Exchange operations abstraction |
| `ICandleRepository.cs` | Interface | Candle data repository |
| `ITradingSignalQueue.cs` | Interface | Signal queue abstraction |
| `IClock.cs` | Interface | Time abstraction |
| `CandleClosedNotification.cs` | Notification | Candle closed event |
| `SmasAndRsisCalculatedEvent.cs` | Notification | Indicators calculated event |
| `TradingSignalNotification.cs` | Notification | Trading signal event |
| `IndicatorConditionMetNotification.cs` | Notification | Indicator condition event |
| `ServiceCollectionExtensions.cs` | DI | `AddApplication()` method |

## Upcoming Changes

- Handlers and strategies will be migrated here from the Host project.
- New interfaces may be added as the domain evolves (e.g., `IEventBus` for event-driven architecture).
- `Class1.cs` is a placeholder — delete it when adding real content.
