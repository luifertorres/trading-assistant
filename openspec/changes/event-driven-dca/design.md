## Context

The current `TradeHandler` is a monolith that cannot support DCA rebuys. The RSI(5) extreme strategy needs to buy multiple blocks on the same symbol as RSI stays below 10, averaging down the entry price. The system must enforce single-symbol exposure and strict capital limits.

## Goals / Non-Goals

**Goals:**

- Decompose TradeHandler into event-driven services
- Enable DCA rebuys on RSI(5) signals
- Enforce single-symbol exposure globally
- Track and limit margin/capital allocation
- Maintain audit trail via domain events

**Non-Goals:**

- Implementing DCA for other strategies (v1 is RSI(5) only, but architecture supports it)
- Stop-loss/take-profit for DCA positions (RSI exit only)
- Multi-symbol concurrent positions
- Persistent event sourcing (optional logging only)
- UI for DCA management

## Decisions

### Decision 1: MediatR as Event Bus

Use MediatR's `IPublisher.Publish()` as the event bus implementation. Domain events implement both `IDomainEvent` and `INotification`. This avoids introducing a new messaging framework.

**Rationale**: MediatR is already in the project. In-process events are sufficient for a single-instance application. If external messaging is needed later, the `IEventBus` abstraction allows swapping implementations.

### Decision 2: Eight Specialized Services

Decompose `TradeHandler` into: IndicatorTracker, Rsi5ExtremeStrategy, ExposureGuard, RebuyPolicy, CapitalAllocator, MarginSupervisor, ExecutionOrchestrator, PositionProjector.

**Rationale**: Single Responsibility Principle. Each service handles one concern and communicates via events. This makes testing trivial — mock the event bus and verify published events.

### Decision 3: In-Memory State for Guards

ExposureGuard and MarginSupervisor maintain state in memory (singleton services). State is reconstructed from exchange on startup.

**Rationale**: Simpler than event sourcing. The exchange is the source of truth for positions and balances. A restart reconstructs state by querying the exchange.

### Decision 4: 5% Block Allocation

Each DCA block is 5% of the current balance with no leverage. This limits maximum exposure to 20 blocks (100% of balance) in the worst case.

**Rationale**: Conservative allocation prevents over-leveraging. The 5% figure is configurable but provides a sensible default.

### Decision 5: Phased Implementation

1. Event infrastructure (bus + event types)
2. Refactor signal flow (Rsi5ExtremeStrategy → EntryRequested)
3. Split TradeHandler into services
4. Persistence (Positions + EventLog tables)
5. DCA validation (RebuyPolicy)
6. Tests
7. Migrate other strategies

**Rationale**: Each phase is independently deployable and testable. The system remains functional between phases.

## Risks / Trade-offs

- **Risk**: Event ordering — if events are processed out of order, state could be inconsistent.
  **Mitigation**: MediatR processes notifications sequentially within a single publish call. No concurrent dispatch issues.

- **Risk**: In-memory state loss on crash — ExposureGuard/MarginSupervisor state is lost.
  **Mitigation**: Reconstruct from exchange positions on startup. The exchange is the source of truth.

- **Trade-off**: Eight services for what was one class adds complexity.
  **Accepted**: The complexity is in the connections (events), not the individual services. Each service is trivially testable.

- **Risk**: Migration period — during phased implementation, both old and new paths may coexist.
  **Mitigation**: Feature flag or strategy-specific routing. RSI(5) uses new path, others use old path until migrated.
