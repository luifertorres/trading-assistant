## Context

Risk management is spread across 7 independent BackgroundService workers that poll positions in loops. Each applies the same logic regardless of strategy, doesn't account for DCA average price changes, and can't compose multiple exit conditions. The event-driven architecture (separate change) provides the event infrastructure needed for a unified risk evaluator.

## Goals / Non-Goals

**Goals:**

- Single `RiskEvaluator` replaces all risk Manager/Worker services
- Per-strategy `RiskProfile` with composable exit conditions
- DCA-aware: risk parameters recalculate on position augmentation
- Event-driven: triggered by price events, not polling
- Configurable via `appsettings.json` per-strategy profiles

**Non-Goals:**

- Portfolio-level risk management (global drawdown limits)
- Dynamic risk adjustment based on market conditions
- Machine learning-based risk optimization
- Risk reporting or analytics dashboard

## Decisions

### Decision 1: RiskProfile as Domain Value Object

`RiskProfile` is a record in the Domain layer with optional config records for each exit type. Strategies declare their profile via `ITradingStrategy.RiskProfile`.

**Rationale**: Risk configuration is a domain concept — it describes how to protect a position. Being a VO makes it immutable and testable. Using optional properties (`StopLossConfig?`) means strategies only declare what they use.

### Decision 2: Priority-Based Evaluation

When multiple conditions trigger simultaneously, a fixed priority order resolves: StopLoss (1) > BreakEven (2) > TrailingStop (3) > TakeProfit (4) > IndicatorExit (5). Only the highest priority action executes.

**Rationale**: StopLoss is the most critical protection and must always win. BreakEven is protective (moves SL to entry). TrailingStop locks in profit. TakeProfit is the target. IndicatorExit is the softest signal.

### Decision 3: Stateful Evaluator with Per-Position State

`RiskEvaluator` maintains a `Dictionary<string, PositionRiskState>` where key is symbol. Each `PositionRiskState` tracks: trailing stop price, break-even activation status, step level. State is reconstructed from exchange on startup.

**Rationale**: Trailing stop and break-even require state (current stop level, activation flag). In-memory state is fast and sufficient — the exchange is the source of truth for position data.

### Decision 4: Configuration Override via IOptions

Risk profiles are bound from `appsettings.json` via `IOptions<RiskManagementOptions>`. Strategies provide defaults; configuration overrides them.

**Rationale**: Allows tuning risk parameters without code changes. The binding pattern is standard .NET options pattern. Defaults in code mean the system works without configuration.

### Decision 5: Phased Migration

1. Create domain types (RiskProfile, configs)
2. Implement RiskEvaluator in Application
3. Integrate with DCA (PositionAugmented handler)
4. Migrate logic from each old worker
5. Configure per-strategy profiles
6. Remove old workers
7. Test

**Rationale**: Each phase is independently testable. Old workers can coexist during migration — disable them one by one as the evaluator takes over.

## Risks / Trade-offs

- **Risk**: Missing price spikes between candle closes could skip stop-loss triggers.
  **Mitigation**: Support optional real-time tick subscription for critical SL/TP levels.

- **Risk**: In-memory state loss on crash could lose trailing stop positions.
  **Mitigation**: Reconstruct from exchange on startup. Optionally persist state to SQLite.

- **Risk**: Interaction between DCA rebuys and trailing stop resets could produce unexpected behavior.
  **Mitigation**: Comprehensive unit tests for all combinations: 3 rebuys with trailing stop active, break-even reset, etc.

- **Trade-off**: Single evaluator is a potential bottleneck vs. parallel workers.
  **Accepted**: The evaluator is CPU-bound and fast. A single evaluation for all positions is simpler and more predictable than concurrent workers.
