## 1. Cleanup

- [ ] 1.1 Delete `TradingAssistant.Application/Class1.cs`
- [ ] 1.2 Delete `TradingAssistant.Infrastructure/Class1.cs`
- [ ] 1.3 Verify build succeeds after deletion

## 2. Move Handlers to Application

- [ ] 2.1 Create `TradingAssistant.Application/Handlers/` directory
- [ ] 2.2 Move `TradeHandler.cs` and `TradeRequest.cs` to Application/Handlers, update namespace
- [ ] 2.3 Move `ClosePositionHandler.cs` and `ClosePositionRequest.cs` to Application/Handlers, update namespace
- [ ] 2.4 Move `RsiCandleClosedHandler.cs` to Application/Handlers, update namespace
- [ ] 2.5 Move `TradingSignalHandler.cs` to Application/Handlers, update namespace
- [ ] 2.6 Move `RsiIndicatorConditionDispatcher.cs` to Application/Handlers, update namespace
- [ ] 2.7 Replace any direct `BinanceService` or FASTER usage with `IExchangeService` / `ICandleRepository` in moved handlers
- [ ] 2.8 Verify build succeeds

## 3. Move Strategies to Application

- [ ] 3.1 Create `TradingAssistant.Application/Strategies/` directory
- [ ] 3.2 Move all `*Strategy.cs` files to Application/Strategies, update namespaces
- [ ] 3.3 Move supporting types (`CandleExtensions.cs`, `QuoteExtensions.cs`, `SymbolExclusions.cs`) if they are strategy dependencies
- [ ] 3.4 Replace any direct infrastructure usage with Application interfaces
- [ ] 3.5 Update `AddApplication()` to scan Application assembly for MediatR handlers
- [ ] 3.6 Remove the duplicate `AddMediatR()` call for Host assembly in `Program.cs`
- [ ] 3.7 Verify build succeeds and live run processes signals correctly

## 4. Exchange Decoupling

- [ ] 4.1 Audit all `BinanceService` usages outside `TradingAssistant.Infrastructure/Binance/`
- [ ] 4.2 Expand `IExchangeService` with any missing methods used by handlers/strategies
- [ ] 4.3 Implement the new methods in `BinanceExchangeService`
- [ ] 4.4 Replace all direct `BinanceService` references in handlers with `IExchangeService`
- [ ] 4.5 Abstract `TriggerLastCandleClosedNotifications()` — create `ExchangeInitializationService : IHostedService` or add to `IExchangeService`
- [ ] 4.6 Update `Program.cs` to remove direct `BinanceService` resolution
- [ ] 4.7 Verify `BinanceService` is only referenced in Infrastructure and Host DI wiring
- [ ] 4.8 Verify build succeeds and live run works correctly

## 5. Remove Binance.Net from Domain

- [ ] 5.1 Create `TimeFrame` enum in Domain (OneMinute, FiveMinutes, FifteenMinutes, ThirtyMinutes, OneHour, FourHours, OneDay, OneWeek, OneMonth)
- [ ] 5.2 Create `OrderSide` enum in Domain (Buy, Sell)
- [ ] 5.3 Create `PositionSide` enum in Domain (Long, Short)
- [ ] 5.4 Update `CandleId` to use `TimeFrame` instead of `KlineInterval`
- [ ] 5.5 Rename `KlineIntervalExtensions` to `TimeFrameExtensions`, update to use domain types
- [ ] 5.6 Create `BinanceTypeMapper` in Infrastructure/Binance with `TimeFrame ↔ KlineInterval`, `OrderSide ↔ Binance.OrderSide` mappings
- [ ] 5.7 Update all Infrastructure code to use mapper for conversions
- [ ] 5.8 Remove `Binance.Net` package reference from `TradingAssistant.Domain.csproj`
- [ ] 5.9 Verify build succeeds across all projects
- [ ] 5.10 Verify live run functions correctly with new type mappings
