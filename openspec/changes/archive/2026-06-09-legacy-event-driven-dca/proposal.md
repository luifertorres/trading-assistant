## Why

The current `TradeHandler` is a monolith that handles entry logic, position sizing, execution, and risk management in a single class. This makes it impossible to implement DCA (Dollar Cost Averaging) rebuys, add new strategies independently, or test components in isolation. The RSI(5) extreme strategy needs multiple rebuys on the same symbol while maintaining strict capital controls.

## What Changes

- Decompose `TradeHandler` into event-driven services: ExposureGuard, CapitalAllocator, ExecutionOrchestrator, MarginSupervisor, PositionProjector.
- Introduce domain events and an event bus abstraction.
- Implement DCA flow: strategy emits intent → guard validates → allocator sizes → orchestrator executes → projector updates state.
- Single-symbol exposure enforcement and balance-based capital limits.

## Capabilities

### New Capabilities

- `event-bus`: In-process event bus abstraction with MediatR backend and optional persistence
- `dca-flow`: Dollar Cost Averaging rebuy flow for RSI(5) extreme strategy with capital allocation guards
- `exposure-guard`: Single-symbol exposure enforcement preventing over-diversification
- `capital-allocation`: Position sizing and margin tracking with 5% block allocation and no leverage

### Modified Capabilities

- `trading-strategies`: `Rsi5ExtremeStrategy` publishes `EntryRequested` events instead of `TradingSignalNotification`
- `exchange-integration`: `IExchangeService` adapted for orders without SL/TP/TSL parameters

## Impact

- `TradingAssistant.Domain/Events/`: New event record types (20+ domain events)
- `TradingAssistant.Application/`: New `IEventBus` interface, new handler services
- `TradingAssistant.Infrastructure/`: `MediatREventBus` implementation, optional `EventLog` table
- `TradingAssistant/Program.cs`: New hosted services registered, old `TradingSignalWorker` replaced
- `TradingAssistant.Infrastructure/Migrations/`: New migration for Positions and EventLog tables
