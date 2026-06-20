## Why

TradingPlatform Research already has `IBacktestRunner`, simulation models, and a `FixedWindow` reference strategy, and MarketData can persist **real** USD-M **1d** history via `backfill-1d`. The CLI `demo` command still seeds **synthetic** 1m bars, so there is no operator path to run a simulation against persisted market data. Without that last mile, the Platform backtest pipeline cannot be validated on real candles before porting alpha strategies.

## What Changes

- Add a **`backtest` CLI subcommand** on `TradingPlatform.Cli` that resolves an instrument from the registry, reads `Day1` bars from `market.sqlite` via `ICandleSeriesReader`, and runs `IBacktestRunner` with a `TradingVectorSpec` and optional date range.
- Emit a **human-readable run summary** (run id, trade count, final equity, max drawdown) to stdout; optionally **persist** the `SimulationRunResult` through `ISimulationRunRepository`.
- Document the **operator workflow**: run `backfill-1d` first (or point at an existing DB), then `backtest` for a symbol such as `BTCUSDT`.
- Keep strategy scope to **existing** `FixedWindowStrategy` (proof of pipeline); no new indicator strategies in this change.

## Capabilities

### New Capabilities

- `trading-platform-research-backtest-cli`: Delivery surface and end-to-end contract for running Research simulations against persisted MarketData candles from the CLI.

### Modified Capabilities

- None. This change **consumes** existing `trading-platform-marketdata-instrument-registry`, `trading-platform-marketdata-candles-store`, and `trading-platform-marketdata-binance-1d-backfill` behavior without altering their requirement sets.

## Impact

- **Code:** `src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli/` (new `backtest` command and argument parsing), minor README updates; no changes required to `BacktestRunner` unless gaps surface during wiring.
- **Dependencies:** Reuses existing Research and MarketData DI extensions (`AddMarketDataSqlite`, `AddResearchInfrastructure`).
- **Data:** Reads `market.sqlite` (candles + instrument registry) and optionally writes `research.sqlite` (simulation runs).
- **Operations:** Requires prior `backfill-1d` (or equivalent data load) for the target symbol and `TimeFrameCode.Day1`; fails clearly when no bars exist in range.
