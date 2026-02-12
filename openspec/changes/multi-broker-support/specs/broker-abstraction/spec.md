## ADDED Requirements

### Requirement: Expanded Exchange Service Interface

`IExchangeService` SHALL cover all trading operations needed by any strategy or handler, independent of broker implementation.

#### Scenario: Market order placed

- **WHEN** a handler needs to place a market order
- **THEN** `IExchangeService.PlaceMarketOrderAsync(symbol, side, quantity, ct)` returns an `ExchangeOrder`
- **AND** the implementation translates to the active broker's API

#### Scenario: Limit order placed

- **WHEN** a handler needs to place a limit order
- **THEN** `IExchangeService.PlaceLimitOrderAsync(symbol, side, price, quantity, ct)` returns an `ExchangeOrder`

#### Scenario: Account snapshot retrieved

- **WHEN** a service needs account balance information
- **THEN** `IExchangeService.GetAccountSnapshotAsync(ct)` returns an `AccountSnapshot` with Balance, AvailableMargin, Equity

#### Scenario: Minimum order size queried

- **WHEN** a sizing service needs exchange constraints
- **THEN** `IExchangeService.GetMinimumOrderSizeAsync(symbol, ct)` returns the minimum order quantity

#### Scenario: Broker name available

- **WHEN** a service needs to identify which broker is active
- **THEN** `IExchangeService.BrokerName` returns the broker identifier (e.g., "Binance", "CapitalCom")

### Requirement: Exchange Stream Service

The system SHALL provide `IExchangeStreamService` for WebSocket-based real-time data feeds.

#### Scenario: Kline subscription

- **WHEN** a service subscribes to candle updates
- **THEN** `IExchangeStreamService.SubscribeToKlineAsync(symbol, timeFrame, onCandle, ct)` delivers candles in real-time
- **AND** uses the active broker's WebSocket implementation

#### Scenario: Order update subscription

- **WHEN** a service subscribes to order updates
- **THEN** `IExchangeStreamService.SubscribeToOrderUpdatesAsync(onOrder, ct)` delivers order fill/rejection events

#### Scenario: Lifecycle management

- **WHEN** the application starts or stops
- **THEN** `IExchangeStreamService.StartAsync(ct)` and `StopAsync(ct)` manage connections cleanly
- **AND** `IAsyncDisposable` ensures resources are released

### Requirement: Type Mapping Pattern

Each broker adapter SHALL include a mapper class that converts between domain types and broker-specific types.

#### Scenario: Mapper translates all types

- **WHEN** infrastructure code handles broker-specific DTOs
- **THEN** a `<Broker>TypeMapper` class provides bidirectional conversion for TimeFrame, OrderSide, OrderType, OrderStatus
- **AND** unmapped values throw `ArgumentOutOfRangeException`

### Requirement: Domain Trading Types

The domain SHALL define broker-agnostic types for orders and account state.

#### Scenario: ExchangeOrder type

- **WHEN** an order result is returned from the exchange
- **THEN** it is represented as `ExchangeOrder` record with: OrderId, Symbol, Side, Type, Price, Quantity, Status, Timestamp

#### Scenario: AccountSnapshot type

- **WHEN** account state is queried
- **THEN** it is represented as `AccountSnapshot` record with: Balance, AvailableMargin, Equity, Timestamp

#### Scenario: Order enums

- **WHEN** order metadata is needed
- **THEN** domain enums `OrderType` (Market, Limit, StopMarket, TakeProfitMarket) and `OrderStatus` (Pending, Filled, PartiallyFilled, Cancelled, Rejected) are available
