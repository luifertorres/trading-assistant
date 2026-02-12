## Why

There is no way to validate trading strategies before deploying them to live trading with real capital. Strategies are developed and tested only in production, which is risky and slow. A backtesting module would allow replaying historical market data through strategies to measure performance metrics (Sharpe ratio, max drawdown, win rate) before going live.

## What Changes

- New domain types: `BacktestRun`, `BacktestResult`, `BacktestConfig`, `BacktestTrade`, `ITradingStrategy` interface.
- New application interfaces: `IBacktestEngine`, `IHistoricalDataProvider`, `ISimulatedExchange`.
- New infrastructure implementation: `BacktestEngine`, historical data fetching from exchange API.
- Strategy abstraction: existing strategies adapted to a common `ITradingStrategy` interface usable by both live and backtest engines.

## Capabilities

### New Capabilities

- `backtesting`: Engine that replays historical candles through strategies and produces performance metrics
- `strategy-abstraction`: Common `ITradingStrategy` interface that works for both live trading and backtesting

### Modified Capabilities

- `trading-strategies`: Existing strategies adapted to implement `ITradingStrategy` for dual use (live + backtest)
- `exchange-integration`: New `IHistoricalDataProvider` interface for fetching historical candle data

## Impact

- `TradingAssistant.Domain/`: New entities (`BacktestRun`, `BacktestTrade`), VOs (`BacktestResult`, `BacktestConfig`), interfaces (`ITradingStrategy`, `IStrategyContext`)
- `TradingAssistant.Application/`: New interfaces (`IBacktestEngine`, `IHistoricalDataProvider`, `ISimulatedExchange`)
- `TradingAssistant.Infrastructure/`: New `BacktestEngine` implementation, `SimulatedExchange`, historical data provider
- `TradingAssistant.Infrastructure/Migrations/`: New migration for `BacktestRuns` table
- Existing strategies: Refactored or adapted to implement `ITradingStrategy`
