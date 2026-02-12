## Context

The trading-assistant is tightly coupled to Binance. The Clean Architecture refactor (separate change) removes `Binance.Net` types from the domain. This change builds on that to create a pluggable multi-broker architecture with Capital.com as the second broker.

## Goals / Non-Goals

**Goals:**

- Expand `IExchangeService` with full trading operations
- Create `IExchangeStreamService` for real-time data feeds
- Refactor Binance adapter to implement expanded interfaces
- Create Capital.com adapter
- Support broker selection via `appsettings.json` configuration
- Ensure strategies work unchanged across brokers

**Non-Goals:**

- Supporting multiple brokers simultaneously (one active at a time)
- Arbitrage between brokers
- Capital.com-specific features (e.g., CFD sentiment data)
- Broker failover/redundancy
- Supporting more than two brokers in v1

## Decisions

### Decision 1: One Active Broker at a Time

The system selects one broker at startup via `Exchange:ActiveBroker`. Only that broker's services are registered in DI. Switching brokers requires a restart.

**Rationale**: Simpler DI, no runtime confusion about which broker is active. Multi-broker concurrent support is a separate concern if ever needed.

### Decision 2: Separate REST and Stream Interfaces

`IExchangeService` handles REST operations (orders, account). `IExchangeStreamService` handles WebSocket streams (candles, order updates). This mirrors how most exchange APIs are structured.

**Rationale**: Different lifecycle — streams are long-lived and need start/stop management, REST calls are fire-and-forget. Separating them allows independent testing and evolution.

### Decision 3: Type Mapper per Broker

Each broker directory contains a static `<Broker>TypeMapper` class with exhaustive pattern-matching `switch` expressions for type conversion.

**Rationale**: Centralizes all type translation. Compiler enforces exhaustiveness — adding a new `TimeFrame` value forces updating all mappers. Easy to audit and test.

### Decision 4: Capital.com REST Client

Create a dedicated `CapitalComApiClient` class that wraps `HttpClient` for Capital.com API calls. This is internal to the Infrastructure layer.

**Rationale**: Capital.com uses a session-based authentication flow (create session → use CST/X-SECURITY-TOKEN headers). A dedicated client encapsulates this complexity.

### Decision 5: Configuration Restructure

Move from `Binance:*` flat config to `Exchange:ActiveBroker` + `Exchange:Binance:*` + `Exchange:CapitalCom:*` structure.

**Rationale**: Clean separation. Broker-specific settings are scoped under their name. Common settings (if any) go under `Exchange:*` root.

## Risks / Trade-offs

- **Risk**: Capital.com API differences — rate limits, order types, WebSocket protocol may differ significantly from Binance.
  **Mitigation**: Research API documentation thoroughly before implementation. Start with candle data and basic orders.

- **Risk**: CFD vs. Futures terminology differences — Capital.com uses CFD concepts while Binance uses Futures/Perpetual.
  **Mitigation**: Domain types are abstract enough (OrderSide, not "Futures OrderSide"). Document terminology mapping in the adapter.

- **Risk**: Capital.com minimum order sizes and precision differ from Binance.
  **Mitigation**: `GetMinimumOrderSizeAsync()` is per-broker. Sizing logic uses this value.

- **Trade-off**: Configuration restructure is a breaking change for existing deployments.
  **Mitigation**: Document migration path. Provide example config in README.
