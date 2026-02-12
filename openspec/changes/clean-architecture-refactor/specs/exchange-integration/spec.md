## MODIFIED Requirements

### Requirement: Exchange Service Abstraction

#### Scenario: Full coverage of exchange operations

- **WHEN** any handler or strategy needs exchange functionality
- **THEN** `IExchangeService` provides the method (no direct `BinanceService` usage)
- **AND** `BinanceService` is only referenced inside `TradingAssistant.Infrastructure/Binance/`

#### Scenario: Startup initialization abstracted

- **WHEN** the application starts and needs to trigger initial candle notifications
- **THEN** it calls an abstracted method (not `BinanceService.TriggerLastCandleClosedNotifications()` directly)
- **AND** the startup trigger is either part of `IExchangeService` or a dedicated hosted service
