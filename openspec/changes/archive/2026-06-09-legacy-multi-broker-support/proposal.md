## Why

The system is currently tied to Binance. Adding Capital.com (or any other broker) requires a broker-agnostic domain and pluggable infrastructure adapters. The domain still has `Binance.Net` types, and configuration assumes a single broker. Supporting multiple brokers unlocks CFD trading, regulatory diversification, and reduces vendor lock-in.

## What Changes

- Expand `IExchangeService` and create `IExchangeStreamService` to cover all trading and streaming operations.
- Create Capital.com adapter alongside the existing Binance adapter.
- Restructure configuration to support broker selection via `Exchange:ActiveBroker`.
- Ensure strategies work without modification across brokers.

## Capabilities

### New Capabilities

- `broker-abstraction`: Expanded exchange interfaces and type mapping pattern for multi-broker support
- `capital-com-adapter`: Capital.com REST and WebSocket adapter implementing exchange interfaces

### Modified Capabilities

- `exchange-integration`: IExchangeService expanded with full trading operations, new IExchangeStreamService
- `architecture`: Configuration restructured from Binance-specific to broker-agnostic

## Impact

- `TradingAssistant.Application/`: Expanded `IExchangeService`, new `IExchangeStreamService`
- `TradingAssistant.Domain/`: New types: `ExchangeOrder`, `AccountSnapshot`, `OrderType`, `OrderStatus`
- `TradingAssistant.Infrastructure/Binance/`: Refactored adapter, new `BinanceTypeMapper`, `BinanceStreamService`
- `TradingAssistant.Infrastructure/CapitalCom/`: New directory with full adapter implementation
- `appsettings.json`: Restructured to `Exchange:ActiveBroker` pattern
- `Program.cs`: Uses `AddExchange()` instead of `AddBinance()`
