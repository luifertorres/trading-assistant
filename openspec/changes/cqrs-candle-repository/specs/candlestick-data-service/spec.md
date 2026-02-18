## ADDED Requirements

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
The Candlestick Data API SHALL expose query endpoints that return locally stored candlesticks filtered by symbol list and time range.

#### Scenario: Query by symbols and interval
- **WHEN** a client requests candles for one or more symbols and a time range
- **THEN** the service returns candles ordered by open time in ascending order
- **AND** the response includes completeness metadata (`isComplete`, `fromOpenTime`, `toOpenTime`, `missingRanges`) for the requested interval

### Requirement: Realtime Closed-Candle Ingestion Ownership
The Candlestick Data API SHALL own realtime ingestion of 1-minute closed candles from Binance Futures websocket streams.

#### Scenario: Closed candle persisted and published
- **WHEN** a 1-minute candle closes for a subscribed symbol
- **THEN** the candle is validated and persisted by the Candlestick Data API
- **AND** downstream consumers can retrieve the updated series through Candlestick Data API query endpoints without direct websocket dependency
