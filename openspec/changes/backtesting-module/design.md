## Context

The trading-assistant runs strategies in real-time against live exchange data. There is no way to validate a strategy's performance against historical data before deploying it. The system already has candle data models, indicator calculations, and strategy logic — the backtesting module reuses these components with a simulated execution layer.

## Goals / Non-Goals

**Goals:**

- Backtest any strategy against historical candle data
- Calculate standard performance metrics (Sharpe, drawdown, win rate, profit factor)
- Reuse existing domain model (Candle, indicators, strategy logic)
- Persist results for comparison
- Export results to CSV

**Non-Goals:**

- Order book simulation (use OHLC prices only in v1)
- Real-time slippage modeling (use fixed basis points)
- Multi-asset portfolio backtesting
- UI for backtest management (CLI or programmatic API only in v1)
- Persistent backtest state between restarts (each run is fresh)

## Decisions

### Decision 1: OHLC-Only Price Model

Use candle close price for market order fills, high/low for stop-loss/take-profit triggers. No order book simulation in v1.

**Rationale**: Simpler implementation, sufficient for strategy validation. Order book data is expensive and complex to model.

### Decision 2: ITradingStrategy as Bridge

Create `ITradingStrategy` interface with `OnCandle(Candle, IStrategyContext)`. Existing strategies get adapter wrappers that translate between MediatR handler pattern and the strategy interface.

**Rationale**: Avoids rewriting all strategies. The adapter pattern allows gradual migration. Live trading continues to use MediatR, backtesting uses the direct interface.

### Decision 3: Simulated Exchange in Infrastructure

`SimulatedExchange` lives in Infrastructure implementing `ISimulatedExchange`. It tracks orders, fills, and positions in memory. No external dependencies.

**Rationale**: The simulated exchange is an infrastructure concern (alternative implementation of exchange operations). It can be independently tested.

### Decision 4: BacktestRun as Aggregate Root

`BacktestRun` is the aggregate root that owns config, status, and result. It enforces invariants (can't set result on a non-completed run, can't re-run a completed run).

**Rationale**: DDD pattern ensures the backtest lifecycle is consistent.

## Risks / Trade-offs

- **Risk**: Strategy adapter complexity — wrapping MediatR handlers to implement `ITradingStrategy` may be non-trivial if handlers have complex DI dependencies.
  **Mitigation**: Start with `Rsi5ExtremeStrategy` which is simplest. Extract pure strategy logic from handlers.

- **Risk**: Historical data availability — exchange APIs may limit how far back data can be fetched.
  **Mitigation**: Cache historical data in FASTER/SQLite for repeated backtests. Download in bulk.

- **Trade-off**: OHLC-only means stop-loss triggers may not accurately reflect intra-candle price movement.
  **Accepted**: For v1 this is sufficient. v2 can add tick-level simulation.

- **Risk**: Performance — 1 year of 1-minute data is ~525,600 candles. Processing must complete in < 5 seconds.
  **Mitigation**: Use struct-based candle type (already exists), minimal allocations, no async in the hot loop.
