## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Realtime Candle-Closed Event Flow

The main app SHALL subscribe to candle-closed events from the Candlestick Data API for timeframes used by active strategies, and SHALL poll for the full candlestick on event receipt before running the strategy pipeline.

#### Scenario: Subscribe to timeframe-specific events and poll on receipt

- **WHEN** the main app has strategies that run on specific timeframes (e.g., 1m, 5m, 1H, 1D)
- **THEN** it subscribes to candle-closed events from the Candlestick Data API for those timeframes only
- **AND** when a candle-closed event is received for a symbol and timeframe of interest, the main app polls the Candlestick Data API for the full candlestick (symbol, timeframe, time range) required by the strategy
- **AND** passes the fetched candlestick to the indicator and strategy pipeline
