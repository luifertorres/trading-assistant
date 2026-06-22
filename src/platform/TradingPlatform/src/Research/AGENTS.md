# Research — Agent Instructions

## Purpose

Trading strategies, backtest simulation, and shared `ITradingStrategy` used by live execution (ADR-003 sink swap).

## Key types

| Type | Project | Role |
|------|---------|------|
| `ITradingStrategy` | Application | `OnBar(BarProcessingContext)` → `OrderIntent` via sink |
| `ITradingStrategyFactory` | Application | Resolves strategy by `TradingVectorSpec.StrategyKind` |
| `IBacktestRunner` | Application | Runs simulation over historical bars |
| `SimulationOrderIntentSink` | Infrastructure | Fills, fees, equity curve |
| `Rsi5ExtremeStrategy` | Infrastructure | RSI(5) cross-up entry; SL = 24h low; TP % for 1x |
| `TradingVectorSpec` | Kernel | Symbol + timeframe + side + strategy kind + parameters |

## Do

- Keep strategies **pure bar logic**; emit `OrderIntent` only.
- Share the same strategy class for backtest and live.
- Persist simulation runs and backtest verdict JSON for gating live arms.

## Don't

- Place orders or call Binance from Research.
- Add Skender/Binance packages to Domain or Application.

## Verify

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backtest --market-db .trading-platform-data/market.sqlite --symbol DOGEUSDT --strategy Rsi5Extreme --timeframe 4H --save
```
