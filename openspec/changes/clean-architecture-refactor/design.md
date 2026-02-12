## Context

The trading-assistant is mid-refactor toward Clean Architecture. Domain, Application, Infrastructure, and Host layers exist but the separation is incomplete: handlers/strategies live in Host, `BinanceService` is referenced directly, and `Binance.Net` types are used in Domain.

## Goals / Non-Goals

**Goals:**

- Move all MediatR handlers and strategies to Application layer
- Expand `IExchangeService` to cover all exchange operations
- Replace `KlineInterval` with domain-owned `TimeFrame` enum
- Remove `Binance.Net` package from Domain project
- Clean up placeholder files

**Non-Goals:**

- Refactoring strategy logic (behavior stays the same)
- Adding new strategies
- Changing the database schema
- Implementing event-driven architecture (separate change)

## Decisions

### Decision 1: Handler and Strategy Placement

Handlers and strategies move to `TradingAssistant.Application/Handlers/` and `TradingAssistant.Application/Strategies/` respectively. The Host's `AddMediatR()` call is consolidated to scan only the Application assembly.

**Rationale**: Strategies are application-layer use cases. They depend on interfaces (IExchangeService, ICandleRepository) which are defined in Application. Keeping them in Host creates an unnecessary layer crossing.

### Decision 2: BinanceTypeMapper for Type Translation

A static `BinanceTypeMapper` class in `Infrastructure/Binance/` handles all conversions between Binance.Net types and domain types. This is the Anti-Corruption Layer.

**Rationale**: Centralizing mappings in one class makes it easy to audit, test, and extend when adding new types. Pattern-matching `switch` expressions provide compile-time exhaustiveness checking.

### Decision 3: Startup Trigger Abstraction

`TriggerLastCandleClosedNotifications()` moves from a direct `BinanceService` call in `Program.cs` to a new `IExchangeStreamService.StartAsync()` method or a dedicated `ExchangeInitializationService : IHostedService`.

**Rationale**: The current code requires resolving `BinanceService` by concrete type in Program.cs, which violates the abstraction principle. A hosted service handles startup initialization cleanly.

### Decision 4: Incremental Migration

The migration follows this strict order to minimize risk:
1. Clean up Class1.cs placeholders (zero risk)
2. Move handlers/strategies to Application (medium risk — may have hidden Host dependencies)
3. Decouple BinanceService references (medium risk — need to expand IExchangeService)
4. Replace Binance.Net types in Domain (high risk — touches CandleId, used everywhere)

## Risks / Trade-offs

- **Risk**: Strategies may have hidden dependencies on Host-layer types or services.
  **Mitigation**: Compile after each move. Fix one-by-one.

- **Risk**: Expanding `IExchangeService` may be complex if `BinanceService` has complex internal state.
  **Mitigation**: Start with the methods already used by handlers. Don't abstract unused methods.

- **Risk**: `KlineInterval` → `TimeFrame` rename touches many files across all layers.
  **Mitigation**: Do this last. Use find-and-replace with compile verification.

- **Trade-off**: The Application assembly will grow significantly with all handlers and strategies.
  **Accepted**: This is correct per Clean Architecture. Future split into sub-namespaces if needed.
