## ADDED Requirements

### Requirement: Common Strategy Interface

All trading strategies SHALL implement a common interface that works for both live trading and backtesting.

#### Scenario: Strategy processes candle

- **WHEN** a candle is available (live stream or historical replay)
- **THEN** `ITradingStrategy.OnCandle(Candle, IStrategyContext)` is called
- **AND** the strategy evaluates its conditions and may request entry/exit via `IStrategyContext`

#### Scenario: Strategy context provides position state

- **WHEN** a strategy needs to check current position
- **THEN** `IStrategyContext.HasOpenPosition` returns whether a position exists
- **AND** `IStrategyContext.CurrentBalance` returns the available balance

#### Scenario: Strategy requests entry

- **WHEN** a strategy detects entry conditions
- **THEN** it calls `IStrategyContext.RequestEntry(OrderSide, decimal quantity)`
- **AND** the context routes the request to either live exchange or simulated exchange

#### Scenario: Strategy requests exit

- **WHEN** a strategy detects exit conditions
- **THEN** it calls `IStrategyContext.RequestExit()`
- **AND** the context routes the exit to either live exchange or simulated exchange

### Requirement: Existing Strategy Adaptation

Existing MediatR-based strategies SHALL be adapted to implement `ITradingStrategy` while maintaining backward compatibility with the current live trading flow.

#### Scenario: Rsi5ExtremeStrategy adapted

- **WHEN** `Rsi5ExtremeStrategy` is used in backtesting
- **THEN** it implements `ITradingStrategy` and evaluates RSI(5) < 10 + bearish candle conditions
- **AND** calls `context.RequestEntry()` instead of publishing MediatR notifications

#### Scenario: Live trading unchanged

- **WHEN** existing strategies run in live mode
- **THEN** they continue to work via MediatR notification handlers as before
- **AND** no breaking changes to the current live trading flow
