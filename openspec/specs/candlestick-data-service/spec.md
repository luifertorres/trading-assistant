> **Status:** Legacy reference — TradingAssistant / CandlestickData. Do not extend for new TradingPlatform features.

## Purpose

Defines the independent Candlestick Data API that owns historical and realtime candlestick ingestion, persistence, and retrieval. This service is the canonical clock for trading and enables the main app to start quickly while market data ingestion runs asynchronously.

## Requirements

### Requirement: Independent Candlestick Data API
The system SHALL provide an independently deployable Candlestick Data API that owns historical and realtime candlestick ingestion, persistence, and retrieval.

#### Scenario: Main app delegates startup data preparation
- **WHEN** the main trading application starts
- **THEN** it calls the Candlestick Data API through a REST client to request data synchronization
- **AND** the API acknowledges the request with an accepted response that includes a synchronization job identifier
- **AND** the main application startup flow continues without waiting for full historical download completion

### Requirement: Historical Sync Lifecycle Control
The Candlestick Data API SHALL expose command endpoints to start or resume, stop, and restart historical candle synchronization jobs.

#### Scenario: Resume from earliest missing candle
- **WHEN** a sync start or resume command is accepted
- **THEN** the service continues from the first chronologically missing candle per symbol and timeframe
- **AND** already stored candles are not duplicated

#### Scenario: Stop and restart long-running sync
- **WHEN** an operator sends stop and later restart commands
- **THEN** the service persists synchronization job state and per-symbol/timeframe checkpoints durably
- **AND** restart resumes deterministically from persisted progress checkpoints

### Requirement: Candlestick Query API
The Candlestick Data API SHALL expose query endpoints that return locally stored candlesticks filtered by symbol list, timeframe, and time range.

#### Scenario: Query by symbols, timeframe, and interval
- **WHEN** a client requests candles for one or more symbols, a timeframe, and a time range
- **THEN** the service returns candles ordered by open time in ascending order
- **AND** the response includes completeness metadata (`isComplete`, `fromOpenTime`, `toOpenTime`, `missingRanges`) for the requested interval

### Requirement: Realtime Closed-Candle Ingestion and Canonical Clock
The Candlestick Data API SHALL own realtime ingestion of closed candles for multiple timeframes (1m, 5m, 15m, 1H, 1D, etc.) from Binance Futures websocket streams and SHALL act as the canonical clock for trading by publishing candle-closed events when candles are persisted.

#### Scenario: Closed candle persisted and candle-closed event published
- **WHEN** a candle of a given timeframe closes for a subscribed symbol
- **THEN** the candle is validated and persisted by the Candlestick Data API
- **AND** the API publishes a candle-closed event (symbol, timeframe, openTime) for downstream consumers to use as the trading clock signal
- **AND** downstream consumers can retrieve the full candlestick series for that symbol and timeframe through query endpoints after the event

#### Scenario: Multiple timeframes emitted
- **WHEN** candles close for different timeframes (e.g., 1m, 5m, 1H, 1D)
- **THEN** the API publishes distinct candle-closed events per timeframe
- **AND** consumers can subscribe only to timeframes relevant to their strategies
