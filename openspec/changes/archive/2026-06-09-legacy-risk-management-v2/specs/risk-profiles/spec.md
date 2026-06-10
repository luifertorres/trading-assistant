## ADDED Requirements

### Requirement: Risk Profile Value Object

Each strategy SHALL declare a `RiskProfile` that defines its risk management parameters.

#### Scenario: Strategy with stop-loss and take-profit

- **WHEN** `MeanReversion1mStrategy` is registered
- **THEN** its `RiskProfile` includes `StopLossConfig(RoiPercentage: 5.0)` and `TakeProfitConfig(RoiPercentage: 3.0)`
- **AND** `BreakEvenConfig(MinRoiBeforeActivation: 1.5)`

#### Scenario: Strategy with trailing stop

- **WHEN** `TrendFollowing1mOr15mStrategy` is registered
- **THEN** its `RiskProfile` includes `TrailingStopConfig(ActivationRoi: 2.0, TrailingPercentage: 1.0)`
- **AND** `StopLossConfig(RoiPercentage: 3.0)`

#### Scenario: Strategy with indicator exit only

- **WHEN** `Rsi5ExtremeStrategy` is registered
- **THEN** its `RiskProfile` includes `IndicatorExitConfig(IndicatorName: "RSI5", Threshold: 90, Direction: Above)`
- **AND** no stop-loss, take-profit, or trailing stop

#### Scenario: Strategy with stepped trailing stop

- **WHEN** a strategy uses stepped trailing stop
- **THEN** its `RiskProfile` includes `TrailingStopConfig(UseSteppedMode: true, Steps: [...])` with a list of `StepConfig(RoiThreshold, TrailingPercentage)`

### Requirement: Risk Profile Configuration

Risk profiles SHALL be configurable via `appsettings.json` with the ability to override strategy defaults.

#### Scenario: Configuration overrides defaults

- **WHEN** `RiskManagement:Profiles:MeanReversion:StopLoss:RoiPercentage` is set in config
- **THEN** it overrides the strategy's default stop-loss value
- **AND** unconfigured parameters fall back to strategy defaults

### Requirement: Composable Exit Conditions

A `RiskProfile` SHALL support multiple exit conditions that are evaluated together with defined priority.

#### Scenario: Priority evaluation order

- **WHEN** multiple exit conditions are triggered simultaneously
- **THEN** the highest priority condition wins: StopLoss > BreakEven > TrailingStop > TakeProfit > IndicatorExit
- **AND** only one exit action is taken per evaluation cycle
