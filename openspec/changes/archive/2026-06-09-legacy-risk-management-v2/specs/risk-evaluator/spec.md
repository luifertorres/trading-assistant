## ADDED Requirements

### Requirement: Unified Risk Evaluator

A single `RiskEvaluator` service SHALL replace all individual Manager/Worker background services for risk management.

#### Scenario: Risk check on price update

- **WHEN** a `RiskCheckRequested` event is received (from candle close or price tick)
- **THEN** `RiskEvaluator` evaluates all active conditions for each open position
- **AND** uses the position's strategy `RiskProfile` to determine which conditions apply

#### Scenario: Stop-loss evaluated

- **WHEN** a position's unrealized loss exceeds the configured `StopLossConfig.RoiPercentage`
- **THEN** `StopLossTriggered` event is published
- **AND** `PositionExitRequested` event follows with reason "StopLoss"

#### Scenario: Take-profit evaluated

- **WHEN** a position's unrealized profit reaches `TakeProfitConfig.RoiPercentage`
- **THEN** `TakeProfitTriggered` event is published
- **AND** `PositionExitRequested` event follows with reason "TakeProfit"

#### Scenario: Trailing stop adjusted

- **WHEN** a position's profit exceeds `TrailingStopConfig.ActivationRoi`
- **THEN** `TrailingStopAdjusted` event is published with the new stop price
- **AND** the stop price only moves in the favorable direction (never back)

#### Scenario: Trailing stop triggered

- **WHEN** price retraces to the trailing stop price
- **THEN** `TrailingStopTriggered` event is published
- **AND** `PositionExitRequested` event follows with reason "TrailingStop"

#### Scenario: Break-even activated

- **WHEN** a position's profit exceeds `BreakEvenConfig.MinRoiBeforeActivation`
- **THEN** `BreakEvenActivated` event is published
- **AND** the effective stop-loss is moved to entry price

#### Scenario: Indicator exit triggered

- **WHEN** the configured indicator crosses its threshold in the specified direction
- **THEN** `IndicatorExitTriggered` event is published
- **AND** `PositionExitRequested` event follows with reason "IndicatorExit"

### Requirement: DCA-Aware Risk Recalculation

Risk parameters SHALL recalculate when a DCA rebuy augments a position.

#### Scenario: Stop-loss recalculated on rebuy

- **WHEN** `PositionAugmented` event is received (new average price)
- **THEN** stop-loss price is recalculated based on the new average entry price
- **AND** take-profit price is recalculated based on the new average

#### Scenario: Trailing stop reset on rebuy

- **WHEN** `PositionAugmented` event is received
- **THEN** trailing stop state is reset to initial (deactivated)
- **AND** it reactivates based on profit from the new average price

#### Scenario: Break-even reset on rebuy

- **WHEN** `PositionAugmented` event is received and break-even was previously activated
- **THEN** break-even state is reset
- **AND** it reactivates based on profit from the new average price

### Requirement: Event-Driven Evaluation (No Polling)

Risk checks SHALL be triggered by events, not by periodic polling.

#### Scenario: Candle close triggers check

- **WHEN** a candle closes for any timeframe
- **THEN** a `RiskCheckRequested` event is published with the close price

#### Scenario: Position change triggers check

- **WHEN** a `PositionAugmented` event is received
- **THEN** an immediate `RiskCheckRequested` is published to re-evaluate with the new average price

#### Scenario: Price tick triggers check (optional)

- **WHEN** a real-time price tick is received (if available)
- **THEN** a `RiskCheckRequested` event is published for critical levels (SL/TP only)
