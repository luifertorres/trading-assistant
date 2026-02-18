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
