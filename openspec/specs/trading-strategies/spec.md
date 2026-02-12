## Purpose

Defines the trading strategy capabilities of the system. Strategies detect technical signals and generate trade requests based on indicator conditions and market data.

## Requirements

### Requirement: Strategy as MediatR Handler

Each trading strategy SHALL be implemented as a MediatR notification handler that subscribes to indicator calculation events.

#### Scenario: Strategy receives indicator data

- **WHEN** `SmasAndRsisCalculatedEvent` is published (after indicator calculation completes)
- **THEN** each registered strategy handler evaluates its entry/exit conditions
- **AND** publishes a `TradingSignalNotification` if conditions are met

### Requirement: Mean Reversion Strategies

The system SHALL support mean reversion strategies that detect oversold conditions and generate buy signals.

#### Scenario: MeanReversion1mOr15m detects entry

- **WHEN** RSI and SMA conditions indicate oversold on 1-minute or 15-minute timeframes
- **THEN** a `TradingSignalNotification` is published with the symbol and entry parameters

#### Scenario: MeanReversion5m detects entry

- **WHEN** RSI and SMA conditions indicate oversold on 5-minute timeframes
- **THEN** a `TradingSignalNotification` is published with the symbol and entry parameters

### Requirement: Trend Following Strategies

The system SHALL support trend-following strategies that detect momentum conditions.

#### Scenario: TrendFollowing1mOr15m detects entry

- **WHEN** SMA crossover and RSI conditions confirm an uptrend on 1-minute or 15-minute timeframes
- **THEN** a `TradingSignalNotification` is published with the symbol and entry parameters

### Requirement: RSI Extreme Strategies

The system SHALL support RSI extreme strategies that detect deeply oversold conditions.

#### Scenario: RSI(5) below 10 on 1-minute

- **WHEN** RSI(5) drops below 10 on the 1-minute timeframe
- **THEN** a `TradingSignalNotification` is published

#### Scenario: RSI(5) below 10 on daily

- **WHEN** RSI(5) drops below 10 on the daily timeframe
- **THEN** a `TradingSignalNotification` is published

#### Scenario: RSI(5) extreme levels

- **WHEN** RSI(5) reaches extreme levels (configurable thresholds)
- **THEN** a `TradeRequest` is sent for immediate execution

### Requirement: Real-Time Indicator Calculation

The system SHALL calculate RSI(5) in real-time between candle closes for faster signal detection.

#### Scenario: RSI(5) real-time update

- **WHEN** a price tick is received between candle closes
- **THEN** `Rsi5RealtimeIndicatorWorker` recalculates RSI(5) with the latest price
- **AND** dispatches `IndicatorConditionMetNotification` if thresholds are crossed

### Requirement: Signal Processing Pipeline

Trading signals SHALL be processed through a queue to ensure sequential execution and prevent race conditions.

#### Scenario: Signal queued and processed

- **WHEN** a strategy publishes a `TradingSignalNotification`
- **THEN** it is enqueued in `ITradingSignalQueue`
- **AND** `TradingSignalWorker` dequeues and processes signals sequentially

### Requirement: Strategy Configuration

Strategy parameters SHALL be configurable via `appsettings.json` without code changes.

#### Scenario: Indicator lengths configured

- **WHEN** the application reads configuration
- **THEN** `Binance:Strategy:LengthA-D` configures indicator lengths per strategy
- **AND** `Binance:Indicators` configures per-timeframe indicator lengths
