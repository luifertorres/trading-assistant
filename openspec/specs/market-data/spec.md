> **Status:** Legacy reference — TradingAssistant / CandlestickData. Do not extend for new TradingPlatform features.

## Purpose

Defines the market data model and technical indicator calculation capabilities that power all trading strategies.

## Requirements

### Requirement: Candle Data Model

The system SHALL represent market data as OHLCV candlestick data using high-performance value types.

#### Scenario: Candle structure

- **WHEN** a candle is created
- **THEN** it contains Symbol, TimeFrame, OpenTime, Open, High, Low, Close, and Volume
- **AND** it is stored as a `struct` for cache-line efficiency

#### Scenario: Candle identity

- **WHEN** a candle needs to be uniquely identified
- **THEN** `CandleId` composite key (Symbol + TimeFrame + OpenTime) is used
- **AND** two candles with the same `CandleId` are considered the same candle

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

### Requirement: RSI Thresholds

The system SHALL define standard RSI thresholds as domain constants.

#### Scenario: Standard thresholds

- **WHEN** a strategy evaluates RSI conditions
- **THEN** `Rsi.Overbought` is 70 and `Rsi.Oversold` is 30
- **AND** additional thresholds are configurable per strategy

### Requirement: Configurable Indicator Lengths

The system SHALL support configurable indicator lengths for different timeframes.

#### Scenario: Per-timeframe configuration

- **WHEN** indicator calculations run
- **THEN** the indicator length is determined by `Binance:Indicators` configuration section
- **AND** different timeframes can have different indicator lengths

### Requirement: Circular Time Series Buffer

The system SHALL use a bounded circular buffer for time-series data to prevent unbounded memory growth.

#### Scenario: Buffer overflow

- **WHEN** more candles are added than the buffer capacity
- **THEN** `CircularTimeSeries<TKey, TValue>` evicts the oldest entries
- **AND** the most recent `CandlestickSize` candles are always available

### Requirement: Price Calculation Utilities

The system SHALL provide domain utilities for common trading price calculations.

#### Scenario: Stop-loss price calculation

- **WHEN** a stop-loss needs to be calculated
- **THEN** `StopLossPrice` computes the correct price based on entry and configured percentage

#### Scenario: Take-profit price calculation

- **WHEN** a take-profit needs to be calculated
- **THEN** `TakeProfitPrice` computes the correct price based on entry and configured percentage

#### Scenario: Stepped trailing stop calculation

- **WHEN** a stepped trailing stop needs adjustment
- **THEN** `SteppedTrailingStop` evaluates the current ROI against configured step thresholds
- **AND** returns the appropriate trailing percentage
