## Why

The Clean Architecture refactoring is partially complete. Handlers and strategies still live in the Host layer, `BinanceService` is directly referenced outside Infrastructure, and `Binance.Net` types leak into the Domain. This violates layer separation rules and blocks future features (backtesting, multi-broker, event-driven DCA) that depend on a clean domain and proper abstractions.

## What Changes

- Move all strategy and handler classes from Host to Application layer.
- Expand `IExchangeService` to cover all exchange operations and eliminate direct `BinanceService` usage.
- Replace `Binance.Net` types in Domain (`KlineInterval`) with domain-owned enums (`TimeFrame`, `OrderSide`, `PositionSide`).
- Remove placeholder `Class1.cs` files from Application and Infrastructure.

## Capabilities

### Modified Capabilities

- `architecture`: Enforcing dependency rule — removing violations in Domain and Host layers
- `exchange-integration`: Expanding `IExchangeService` to cover all exchange operations
- `trading-strategies`: Strategies move from Host to Application (namespace change, same behavior)
- `market-data`: `CandleId` changes from `KlineInterval` to domain-owned `TimeFrame` enum

## Impact

- `TradingAssistant.Domain/KlineIntervalExtensions.cs`: Replaced with `TimeFrameExtensions.cs`
- `TradingAssistant.Domain/CandleId.cs`: Uses `TimeFrame` instead of `KlineInterval`
- `TradingAssistant.Domain.csproj`: Removes `Binance.Net` package reference
- `TradingAssistant.Application/Strategies/`: New directory for moved strategies
- `TradingAssistant.Application/Handlers/`: New directory for moved handlers
- `TradingAssistant/Program.cs`: Simplified DI — single `AddMediatR()` call scanning Application
- `TradingAssistant.Infrastructure/Binance/BinanceTypeMapper.cs`: New mapper for `TimeFrame ↔ KlineInterval`
