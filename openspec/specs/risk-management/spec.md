## Purpose

Defines the risk management capabilities that protect positions from excessive loss and lock in profits. Currently implemented as independent BackgroundService workers.

## Requirements

### Requirement: Stop-Loss Protection

The system SHALL automatically close positions that exceed a configured loss threshold.

#### Scenario: Stop-loss triggered

- **WHEN** a position's unrealized loss exceeds `RiskManagement:StopLossRoi` percentage
- **THEN** `StopLossManager` sends a close order via `IExchangeService`
- **AND** the position is closed at market price

### Requirement: Take-Profit Target

The system SHALL automatically close positions that reach a configured profit target.

#### Scenario: Take-profit triggered

- **WHEN** a position's unrealized profit reaches `RiskManagement:TakeProfitRoi` percentage
- **THEN** `TakeProfitManager` sends a close order via `IExchangeService`

### Requirement: Break-Even Protection

The system SHALL move the stop-loss to entry price once a position reaches a minimum profit threshold.

#### Scenario: Break-even activated

- **WHEN** a position's unrealized profit exceeds `RiskManagement:MinRoiBeforeBreakEven` percentage
- **THEN** `BreakEvenWorker` adjusts the stop-loss level to the entry price

### Requirement: Trailing Stop

The system SHALL support a dynamic trailing stop that follows price movement upward.

#### Scenario: Trailing stop adjusts

- **WHEN** a position's price moves favorably beyond the trailing activation threshold
- **THEN** `TrailingStopManager` adjusts the stop-loss upward to trail the price
- **AND** the stop-loss never moves downward

### Requirement: Stepped Trailing Stop

The system SHALL support a stepped trailing stop with configurable ROI thresholds and trailing percentages.

#### Scenario: Step threshold reached

- **WHEN** a position's ROI crosses a configured step threshold
- **THEN** `SteppedTrailingStopManager` applies the corresponding trailing percentage
- **AND** each higher step tightens the trailing percentage

### Requirement: Indicator-Based Exit

The system SHALL support closing positions based on indicator conditions.

#### Scenario: EMA(5) close condition

- **WHEN** the EMA(5) cross condition is met for an open position
- **THEN** `Ema5ClosePositionWorker` closes the position

#### Scenario: RSI(200) close condition

- **WHEN** RSI(200) reaches a configured threshold for an open position
- **THEN** `Rsi200ClosePositionWorker` closes the position

### Requirement: Risk Configuration

Risk management parameters SHALL be configurable via `appsettings.json`.

#### Scenario: Configuration loaded

- **WHEN** the application starts
- **THEN** `Binance:RiskManagement:AccountMarginPercentage` controls position sizing
- **AND** `Binance:RiskManagement:StopLossRoi` controls stop-loss threshold
- **AND** `Binance:RiskManagement:TakeProfitRoi` controls take-profit threshold
- **AND** `Binance:RiskManagement:MinRoiBeforeBreakEven` controls break-even activation
