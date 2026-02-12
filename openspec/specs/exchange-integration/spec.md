## Purpose

Defines the exchange integration capabilities. The system connects to Binance Futures via REST and WebSocket APIs, abstracted behind `IExchangeService` to support future multi-broker scenarios.

## Requirements

### Requirement: Exchange Service Abstraction

All exchange operations SHALL be accessed through the `IExchangeService` interface defined in the Application layer.

#### Scenario: Trading via abstraction

- **WHEN** a handler or strategy needs to interact with the exchange
- **THEN** it uses `IExchangeService` methods (not `BinanceService` directly)
- **AND** the Infrastructure layer provides the concrete implementation

### Requirement: Real-Time Candle Streaming

The system SHALL receive candle updates in real-time via WebSocket connections.

#### Scenario: Candle stream connected

- **WHEN** the application starts
- **THEN** `BinanceService` establishes WebSocket connections for configured symbols
- **AND** publishes `CandleClosedNotification` via MediatR when a candle closes

### Requirement: Historical Candle Loading

The system SHALL load historical candles on startup to initialize indicators.

#### Scenario: Initial candle load

- **WHEN** the application starts
- **THEN** it fetches the last `CandlestickSize` candles (configured, default 2200) from the REST API
- **AND** stores them in `ICandleRepository` (FASTER cache)
- **AND** triggers initial indicator calculations

### Requirement: Order Execution

The system SHALL execute market and limit orders on Binance Futures.

#### Scenario: Market order placed

- **WHEN** a trade signal is processed by `TradeHandler`
- **THEN** `IExchangeService` places a market order with the calculated position size
- **AND** returns the fill result

### Requirement: Position Management

The system SHALL track open positions and support position closing.

#### Scenario: Position persisted

- **WHEN** a position is opened or modified
- **THEN** `PositionWriterWorker` persists the position to SQLite via `TradingContext`

#### Scenario: Position closed

- **WHEN** a close request is received (via risk management or strategy exit)
- **THEN** `ClosePositionHandler` closes the position via `IExchangeService`

### Requirement: High-Performance Candle Cache

The system SHALL use FASTER (FasterKV) for high-performance candle storage to avoid GC pressure during real-time processing.

#### Scenario: Candle stored and retrieved

- **WHEN** a candle is received from the exchange
- **THEN** `FasterCandleRepository` stores it keyed by `CandleId` (Symbol + TimeFrame + OpenTime)
- **AND** retrieval is sub-millisecond for indicator calculations

### Requirement: Telegram Notifications

The system SHALL send trading notifications and errors to a configured Telegram channel.

#### Scenario: Trade notification sent

- **WHEN** a significant event occurs (trade executed, error, position closed)
- **THEN** a log message is sent to Telegram via the configured logging provider
