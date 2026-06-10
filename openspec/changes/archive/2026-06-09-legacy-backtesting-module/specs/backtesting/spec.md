## ADDED Requirements

### Requirement: Backtest Run Lifecycle

The system SHALL support creating, executing, and completing backtest runs against historical data.

#### Scenario: Backtest created and executed

- **WHEN** a user specifies a strategy, symbol, timeframe, date range, and config
- **THEN** a `BacktestRun` aggregate is created with status `Pending`
- **AND** the engine fetches historical data via `IHistoricalDataProvider`
- **AND** candles are iterated chronologically through the strategy
- **AND** status transitions to `Running`, then `Completed`

#### Scenario: Insufficient historical data

- **WHEN** the date range contains fewer candles than the minimum required for indicator calculation
- **THEN** a `DomainException` is thrown with message "Insufficient data: need at least {n} candles for {indicator}"

#### Scenario: Date range with no trades

- **WHEN** the strategy generates no signals during the entire date range
- **THEN** a `BacktestResult` is returned with zero trades and neutral metrics

### Requirement: Performance Metrics Calculation

The system SHALL calculate standard trading performance metrics from backtest results.

#### Scenario: Metrics computed after backtest

- **WHEN** a backtest completes
- **THEN** `BacktestResult` contains: TotalTrades, WinRate, NetProfit, MaxDrawdown, SharpeRatio, ProfitFactor
- **AND** `AverageWin`, `AverageLoss`, `MaxConsecutiveLosses` are calculated
- **AND** an equity curve (list of `EquityPoint`) is generated

#### Scenario: Win rate calculation

- **WHEN** a backtest has winning and losing trades
- **THEN** WinRate = WinningTrades / TotalTrades (as decimal 0-1)

### Requirement: Simulated Exchange

The system SHALL simulate order execution against historical candle data without connecting to a real exchange.

#### Scenario: Market order fills at close price

- **WHEN** a strategy requests a market order during backtesting
- **THEN** `ISimulatedExchange` fills the order at the candle's close price

#### Scenario: Stop-loss triggered by candle low

- **WHEN** a position has a stop-loss set and the candle's low reaches the stop price
- **THEN** the position is closed at the stop price

#### Scenario: Balance depleted

- **WHEN** the simulated account balance is insufficient for a new order
- **THEN** the order is rejected and the backtest continues processing remaining candles

### Requirement: Backtest Persistence

Completed backtest results SHALL be persisted for later retrieval and comparison.

#### Scenario: Result saved to database

- **WHEN** a backtest completes
- **THEN** the `BacktestRun` (including `BacktestResult`) is persisted in SQLite

#### Scenario: Results retrievable

- **WHEN** a user requests past backtest results
- **THEN** results can be queried by strategy name, symbol, or date range

### Requirement: CSV Export

The system SHALL support exporting backtest results to CSV format.

#### Scenario: Trade list exported

- **WHEN** a user requests export of a completed backtest
- **THEN** a CSV file is generated with columns: Symbol, Side, EntryPrice, ExitPrice, EntryTime, ExitTime, Quantity, PnL, PnLPercentage, ExitReason
- **AND** a summary section with aggregate metrics is included
