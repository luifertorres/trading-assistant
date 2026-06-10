## 1. Domain Types

- [ ] 1.1 Create `OrderType` enum in Domain: Market, Limit, StopMarket, TakeProfitMarket
- [ ] 1.2 Create `OrderStatus` enum in Domain: Pending, Filled, PartiallyFilled, Cancelled, Rejected
- [ ] 1.3 Create `ExchangeOrder` record in Domain: OrderId, Symbol, Side, Type, Price, Quantity, Status, Timestamp
- [ ] 1.4 Create `AccountSnapshot` record in Domain: Balance, AvailableMargin, Equity, Timestamp

## 2. Interface Expansion

- [ ] 2.1 Expand `IExchangeService` with: PlaceMarketOrderAsync, PlaceLimitOrderAsync, CancelOrderAsync, GetOpenPositionsAsync, ClosePositionAsync, GetAccountSnapshotAsync, SetLeverageAsync, GetMinimumOrderSizeAsync, BrokerName property
- [ ] 2.2 Create `IExchangeStreamService` interface: StartAsync, StopAsync, SubscribeToKlineAsync, SubscribeToOrderUpdatesAsync, IAsyncDisposable
- [ ] 2.3 Update `GetCandlesAsync` signature to use domain `TimeFrame` and return domain `Candle` types

## 3. Refactor Binance Adapter

- [ ] 3.1 Create `BinanceTypeMapper` with exhaustive mappings for TimeFrame, OrderSide, OrderType, OrderStatus
- [ ] 3.2 Implement expanded `IExchangeService` methods in `BinanceExchangeService`
- [ ] 3.3 Create `BinanceStreamService` implementing `IExchangeStreamService` — extract WebSocket logic from `BinanceService`
- [ ] 3.4 Create `BinanceConfiguration` class for strongly-typed Binance settings
- [ ] 3.5 Update `AddBinance()` DI method to register new services
- [ ] 3.6 Verify live trading works with refactored Binance adapter

## 4. Capital.com Adapter

- [ ] 4.1 Research Capital.com API: REST endpoints, WebSocket protocol, authentication flow
- [ ] 4.2 Create `CapitalComApiClient` — HttpClient wrapper with session-based auth (CST + X-SECURITY-TOKEN)
- [ ] 4.3 Create `CapitalComTypeMapper` with mappings for Capital.com types
- [ ] 4.4 Implement `CapitalComExchangeService` — all IExchangeService methods
- [ ] 4.5 Implement `CapitalComStreamService` — IExchangeStreamService with Capital.com streaming
- [ ] 4.6 Create `CapitalComConfiguration` class for strongly-typed settings
- [ ] 4.7 Create `AddCapitalCom()` DI extension method

## 5. Configuration and DI

- [ ] 5.1 Restructure `appsettings.json` to `Exchange:ActiveBroker` + `Exchange:Binance:*` + `Exchange:CapitalCom:*`
- [ ] 5.2 Create `AddExchange(IConfiguration)` method that selects adapter based on `Exchange:ActiveBroker`
- [ ] 5.3 Update `Program.cs` to use `AddExchange()` instead of `AddBinance()`
- [ ] 5.4 Update README.md with new configuration structure

## 6. Testing

- [ ] 6.1 Unit test: `BinanceTypeMapper` — all type conversions both directions
- [ ] 6.2 Unit test: `CapitalComTypeMapper` — all type conversions both directions
- [ ] 6.3 Integration test: Binance adapter connects and fetches candles
- [ ] 6.4 Integration test: Capital.com adapter connects to demo and fetches candles
- [ ] 6.5 Verify strategy execution is identical across both brokers using backtesting

## 7. Verification

- [ ] 7.1 Build succeeds with both adapters compiled
- [ ] 7.2 Switching `Exchange:ActiveBroker` between "Binance" and "CapitalCom" works with config change only
- [ ] 7.3 Live trading on Binance functions correctly with new configuration structure
