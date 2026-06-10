## Why

Risk management is currently implemented as independent polling-based BackgroundService workers. Each worker applies the same rules to all positions regardless of which strategy opened them. This causes problems with DCA (stop-loss calculated on initial entry ignores rebuy average price updates), prevents combining exit conditions (e.g., "trailing stop OR RSI exit"), and makes testing difficult due to infinite-loop workers.

## What Changes

- Replace independent Manager/Worker services with a single event-driven `RiskEvaluator`.
- Introduce `RiskProfile` value object that each strategy declares, defining its risk parameters.
- Support per-strategy risk profiles: MeanReversion gets SL/TP/BreakEven, TrendFollowing gets TrailingStop, RSI(5) gets indicator-based exit only.
- DCA-aware risk recalculation: when a position is augmented, stop-loss/take-profit recalculate based on new average price.
- Move from polling to event-driven evaluation (triggered on price updates and position changes).

## Capabilities

### New Capabilities

- `risk-profiles`: Per-strategy risk configuration with composable exit conditions
- `risk-evaluator`: Unified event-driven risk evaluation service replacing all individual managers/workers

### Modified Capabilities

- `risk-management`: Existing risk logic migrated from polling workers to event-driven evaluator
- `trading-strategies`: Each strategy declares a `RiskProfile` property

## Impact

- `TradingAssistant.Domain/`: New `RiskProfile`, `StopLossConfig`, `TakeProfitConfig`, `TrailingStopConfig`, `BreakEvenConfig`, `IndicatorExitConfig` records; risk events
- `TradingAssistant.Application/`: New `RiskEvaluator` service
- `TradingAssistant/`: Remove `StopLossManager`, `TakeProfitManager`, `TrailingStopManager`, `SteppedTrailingStopManager`, `BreakEvenWorker`, `Ema5ClosePositionWorker`, `Rsi200ClosePositionWorker`
- `appsettings.json`: New `RiskManagement:Profiles` section with per-strategy config
