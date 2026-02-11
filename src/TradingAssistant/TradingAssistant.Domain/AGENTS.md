# Domain Layer - Agent Instructions

## Purpose

This is the **innermost layer** of the Clean Architecture. It contains the core business logic, entities, value objects, and domain utilities. It must have **zero dependencies** on infrastructure, frameworks, or external libraries.

## Rules

### Dependency Constraints

- **NEVER** add NuGet packages for infrastructure concerns (EF Core, Binance.Net, HTTP clients, etc.).
- **NEVER** reference `TradingAssistant.Application`, `TradingAssistant.Infrastructure`, or the Host project.
- The only acceptable external dependency is `Binance.Net` **temporarily** — it is scheduled for removal. Do not add new usages of it.
- Pure .NET BCL dependencies only (System.*, Microsoft.Extensions.Primitives if needed).

### Entities

- Entities must have **behavior** (methods that enforce invariants), not just properties.
- Use private setters or `init` for properties that shouldn't change after creation.
- Example: `OpenPosition` — tracks an active trading position with its lifecycle.

### Value Objects

- Use `record` for reference-type VOs (e.g., `CandlestickGap`).
- Use `readonly record struct` or `readonly struct` for high-performance VOs (e.g., `Candle`, `CandleId`, `StopLossPrice`, `TakeProfitPrice`).
- Value Objects must be **immutable** — all state set at construction.
- Include domain validation in constructors or factory methods.

### Domain Constants

- Group related constants in static classes (e.g., `Rsi` for RSI thresholds, `Length` for indicator periods).
- Use `const` for compile-time constants, `static readonly` for computed values.

### Extension Methods

- Use for conversions and utilities (e.g., `KlineIntervalExtensions`, `NumberExtensions`).
- Keep extensions pure — no side effects, no I/O.

### Collections

- `CircularTimeSeries<TKey, TValue>` — domain-specific circular buffer for time-series data. Use this for bounded candle/indicator history.

## Current Contents

| File | Type | Description |
|------|------|-------------|
| `Candle.cs` | Struct | OHLCV candlestick data |
| `CandleId.cs` | Struct | Composite key (Symbol, TimeFrame, OpenTime) |
| `OpenPosition.cs` | Entity | Active trading position |
| `Rsi.cs` | Constants | RSI thresholds (Overbought=70, Oversold=30) |
| `StopLossPrice.cs` | Value Object | Stop-loss price calculation |
| `TakeProfitPrice.cs` | Value Object | Take-profit price calculation |
| `SteppedTrailingStop.cs` | Value Object | Stepped trailing stop logic |
| `CandlestickGap.cs` | Value Object | Gap detection |
| `CircularTimeSeries.cs` | Collection | Bounded time-series buffer |
| `Length.cs` | Constants | Common indicator lengths |
| `KlineIntervalExtensions.cs` | Extensions | Timeframe conversions (to be refactored — remove Binance.Net dependency) |
| `NumberExtensions.cs` | Extensions | Numeric utilities |

## Upcoming Changes

- Replace `KlineInterval` (Binance.Net) with own `TimeFrame` enum.
- Add `OrderSide`, `PositionSide` enums owned by the domain.
- Move domain events to `Events/` subfolder when event-driven architecture is implemented.
