## ADDED Requirements

### Requirement: Candle Gap Detection and Classification
The system SHALL detect missing candles and classify each gap as normal exchange behavior or atypical data loss using Binance Futures kline interval continuity as the reference truth.

#### Scenario: Normal market gap is classified as expected
- **WHEN** a symbol or interval has no candles during an exchange-recognized non-trading condition (for example maintenance windows, symbol lifecycle pauses, or explicit exchange incident periods)
- **THEN** the gap is classified as normal
- **AND** the symbol and timeframe integrity status remains `Eligible`

#### Scenario: Atypical gap is classified as data integrity issue
- **WHEN** one or more candles are missing where Binance Futures indicates candles should exist
- **THEN** the gap is classified as atypical
- **AND** the symbol and timeframe integrity status is set to `Compromised` until remediation

### Requirement: Integrity Gate for Strategy Eligibility
The system SHALL expose per-symbol and timeframe candle integrity status (`Eligible`, `Compromised`, `Recovering`) so trading signal processing can be inhibited during atypical gaps.

#### Scenario: Signal processing blocked during atypical gap
- **WHEN** a strategy attempts to evaluate signals for a symbol and timeframe with integrity status `Compromised`
- **THEN** signal generation and execution are blocked for that symbol and timeframe
- **AND** the block reason indicates active atypical candle gap status

### Requirement: Gap Remediation Workflow
The system SHALL provide deterministic remediation to backfill atypical gaps and clear integrity-compromised status after verification.

#### Scenario: Gap backfill restores eligibility
- **WHEN** missing candles are downloaded and continuity is revalidated
- **THEN** the atypical gap is marked remediated
- **AND** continuity verification confirms there are no missing candle open times for the remediated interval
- **AND** the integrity status transitions from `Recovering` to `Eligible` for strategy processing
