# Research — Agent Instructions

## Purpose

Trading strategies, backtest simulation, and shared `ITradingStrategy` used by live execution (ADR-003 sink swap).

## Key types

| Type | Project | Role |
|------|---------|------|
| `ITradingStrategy` | Application | `OnBar(BarProcessingContext)` → `OrderIntent` via sink |
| `ITradingStrategyFactory` | Application | Resolves strategy by `TradingVectorSpec.TradingLogic` |
| `IBacktestRunner` | Application | Runs simulation over historical bars |
| `SimulationOrderIntentSink` | Infrastructure | Fills, fees, equity curve; one position per vector |
| `Sma200Sma5Strategy` | Infrastructure | Scherman SMA200/SMA5 long/short; no within-vector pyramid |
| `Rsi5ExtremeStrategy` | Infrastructure | RSI(5) cross-up entry; SL = 24h low; TP % for 1x |
| `TradingVectorSpec` | Kernel | Asset + Direction + TimeFrame + TradingLogic + parameters |
| `VectorInventory` | Kernel | Per-vector tracked qty (hedge-mode accounting) |

## Do

- Keep strategies **pure bar logic**; emit `OrderIntent` only.
- Share the same strategy class for backtest and live.
- Size each vector with `VectorRiskFraction` × `InitialCapital`.
- Persist simulation runs and backtest verdict JSON for gating live arms.

## Don't

- Place orders or call Binance from Research.
- Add Skender/Binance packages to Domain or Application.
- Pyramid within a single TradingVector.

## Verify

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backtest --market-db .trading-platform-data/market.sqlite --symbol BTCUSDT --trading-logic Sma200Sma5 --direction Long --timeframe 1D --vector-risk 0.02
```
