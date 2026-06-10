> **Status:** Legacy reference — TradingAssistant / CandlestickData. Do not extend for new TradingPlatform features.

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

The system SHALL delegate historical candle synchronization to the Candlestick Data API, while transport-level rate-limit and back-off mechanics remain the responsibility of `Binance.Net` and `CryptoExchange.Net`.

#### Scenario: Startup delegates historical sync

- **WHEN** the application starts
- **THEN** it requests asynchronous historical synchronization through the Candlestick Data API
- **AND** the Candlestick Data API acknowledges the request with an accepted response containing a synchronization job identifier
- **AND** it does not block startup on downloading the last `CandlestickSize` candles directly from Binance REST
- **AND** initial indicator calculations only run for symbols with available local data and integrity status `Eligible`

#### Scenario: Resumable synchronization under client-managed throttling

- **WHEN** historical download throughput is reduced by exchange constraints handled by the client libraries
- **THEN** the Candlestick Data API preserves resumable progress and idempotent writes
- **AND** after interruption or restart, synchronization resumes from the persisted per-symbol/timeframe checkpoint instead of restarting from scratch

### Requirement: Realtime Candle-Closed Event Flow

The main app SHALL subscribe to candle-closed events from the Candlestick Data API for timeframes used by active strategies, and SHALL poll for the full candlestick on event receipt before running the strategy pipeline.

#### Scenario: Subscribe to timeframe-specific events and poll on receipt

- **WHEN** the main app has strategies that run on specific timeframes (e.g., 1m, 5m, 1H, 1D)
- **THEN** it subscribes to candle-closed events from the Candlestick Data API for those timeframes only
- **AND** when a candle-closed event is received for a symbol and timeframe of interest, the main app polls the Candlestick Data API for the full candlestick (symbol, timeframe, time range) required by the strategy
- **AND** passes the fetched candlestick to the indicator and strategy pipeline

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
