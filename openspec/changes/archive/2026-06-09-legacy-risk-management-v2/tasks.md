## 1. Domain Types

- [ ] 1.1 Create `ThresholdDirection` enum: Above, Below
- [ ] 1.2 Create `StopLossConfig` record: RoiPercentage
- [ ] 1.3 Create `TakeProfitConfig` record: RoiPercentage
- [ ] 1.4 Create `BreakEvenConfig` record: MinRoiBeforeActivation
- [ ] 1.5 Create `StepConfig` record: RoiThreshold, TrailingPercentage
- [ ] 1.6 Create `TrailingStopConfig` record: ActivationRoi, TrailingPercentage, UseSteppedMode, Steps
- [ ] 1.7 Create `IndicatorExitConfig` record: IndicatorName, Threshold, Direction
- [ ] 1.8 Create `RiskProfile` record: Name, StopLoss?, TakeProfit?, TrailingStop?, BreakEven?, IndicatorExit?
- [ ] 1.9 Create risk events in Domain/Events: RiskCheckRequested, StopLossTriggered, TakeProfitTriggered, TrailingStopAdjusted, TrailingStopTriggered, BreakEvenActivated, IndicatorExitTriggered, PositionExitRequested

## 2. Strategy Risk Profiles

- [ ] 2.1 Add `RiskProfile` property to `ITradingStrategy` interface
- [ ] 2.2 Define default RiskProfile for MeanReversion strategies: SL(5%), TP(3%), BreakEven(1.5%)
- [ ] 2.3 Define default RiskProfile for TrendFollowing strategy: TrailingStop(activation: 2%, trail: 1%), SL(3%)
- [ ] 2.4 Define default RiskProfile for Rsi5ExtremeStrategy: IndicatorExit(RSI5, 90, Above)

## 3. RiskEvaluator Service

- [ ] 3.1 Create `RiskEvaluator` in Application — subscribes to RiskCheckRequested
- [ ] 3.2 Implement stop-loss evaluation: compare unrealized loss vs RoiPercentage
- [ ] 3.3 Implement take-profit evaluation: compare unrealized profit vs RoiPercentage
- [ ] 3.4 Implement break-even activation: move SL to entry when profit > MinRoiBeforeActivation
- [ ] 3.5 Implement trailing stop: track highest profit, trigger when price retraces by TrailingPercentage
- [ ] 3.6 Implement stepped trailing stop: evaluate current step based on ROI thresholds
- [ ] 3.7 Implement indicator exit: evaluate indicator value against threshold and direction
- [ ] 3.8 Implement priority resolution: StopLoss > BreakEven > TrailingStop > TakeProfit > IndicatorExit
- [ ] 3.9 Create `PositionRiskState` internal class: trailing stop price, break-even flag, current step level

## 4. DCA Integration

- [ ] 4.1 Subscribe to `PositionAugmented` in RiskEvaluator
- [ ] 4.2 Recalculate stop-loss and take-profit based on new average entry price
- [ ] 4.3 Reset trailing stop state to initial (deactivated) on augmentation
- [ ] 4.4 Reset break-even state on augmentation
- [ ] 4.5 Trigger immediate risk re-evaluation after augmentation

## 5. Migration from Old Workers

- [ ] 5.1 Migrate `StopLossManager` logic into RiskEvaluator stop-loss evaluation
- [ ] 5.2 Migrate `TakeProfitManager` logic into RiskEvaluator take-profit evaluation
- [ ] 5.3 Migrate `TrailingStopManager` logic into RiskEvaluator trailing stop
- [ ] 5.4 Migrate `SteppedTrailingStopManager` logic into RiskEvaluator stepped trailing stop
- [ ] 5.5 Migrate `BreakEvenWorker` logic into RiskEvaluator break-even
- [ ] 5.6 Migrate `Ema5ClosePositionWorker` as an indicator exit (IndicatorExitConfig)
- [ ] 5.7 Migrate `Rsi200ClosePositionWorker` as an indicator exit (IndicatorExitConfig)
- [ ] 5.8 Remove old Manager/Worker background services from Host
- [ ] 5.9 Remove old hosted service registrations from Program.cs

## 6. Configuration

- [ ] 6.1 Create `RiskManagementOptions` class with Profiles dictionary
- [ ] 6.2 Add `RiskManagement:Profiles` section to `appsettings.json`
- [ ] 6.3 Bind configuration to `IOptions<RiskManagementOptions>` in DI
- [ ] 6.4 Implement config-override-defaults logic in strategy profile resolution

## 7. Testing

- [ ] 7.1 Unit test: Stop-loss triggers at exact ROI boundary
- [ ] 7.2 Unit test: Take-profit triggers at exact ROI boundary
- [ ] 7.3 Unit test: Trailing stop adjusts correctly over a price series
- [ ] 7.4 Unit test: Stepped trailing stop transitions between steps
- [ ] 7.5 Unit test: Break-even activates and moves stop-loss to entry
- [ ] 7.6 Unit test: Priority resolution — SL wins over TP when both trigger
- [ ] 7.7 Unit test: DCA augmentation recalculates SL based on new average
- [ ] 7.8 Unit test: DCA augmentation resets trailing stop state
- [ ] 7.9 Integration test: Full position lifecycle with risk events
- [ ] 7.10 Verify build succeeds and live run demonstrates correct risk management
