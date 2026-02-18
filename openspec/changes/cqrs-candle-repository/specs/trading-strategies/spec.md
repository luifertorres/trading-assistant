## MODIFIED Requirements

### Requirement: Signal Processing Pipeline

Trading signals SHALL be processed sequentially to prevent race conditions, and SHALL be blocked for symbol and timeframe streams with active atypical candle gaps.

#### Scenario: Signal queued and processed when integrity is eligible

- **WHEN** a strategy publishes a `TradingSignalNotification` for a symbol and timeframe with integrity status `Eligible`
- **THEN** it is enqueued in `ITradingSignalQueue`
- **AND** the signal processing pipeline dequeues and processes signals sequentially

#### Scenario: Signal inhibited when atypical gap is active

- **WHEN** a strategy publishes a `TradingSignalNotification` for a symbol and timeframe with integrity status `Compromised`
- **THEN** the signal is not executed
- **AND** the system records the signal as inhibited with reason code `CANDLE_INTEGRITY_COMPROMISED`
