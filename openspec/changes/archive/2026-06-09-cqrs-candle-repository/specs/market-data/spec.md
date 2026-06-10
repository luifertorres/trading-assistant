## MODIFIED Requirements

### Requirement: Technical Indicator Calculation

The system SHALL calculate RSI and SMA technical indicators on validated candle data sourced from the Candlestick Data API.

#### Scenario: Indicators calculated when candle-closed event triggers fetch

- **WHEN** the main app receives a candle-closed event from the Candlestick Data API for a symbol and timeframe with integrity status `Eligible`
- **AND** the main app fetches the full candlestick series for that symbol and timeframe from the Candlestick Data API
- **THEN** the indicator calculation pipeline calculates RSI and SMA values for configured lengths on the fetched data
- **AND** publishes an indicator-calculated notification with the computed values

#### Scenario: Indicator calculation inhibited on atypical gap

- **WHEN** a symbol and timeframe are marked with integrity status `Compromised` because of an atypical candle gap
- **THEN** indicator calculation for that symbol and timeframe is inhibited
- **AND** no indicator-calculated notification is published for that symbol and timeframe
