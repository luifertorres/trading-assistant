# Research — Agent Instructions

## Purpose

Trading strategies, backtest simulation, and shared `ITradingStrategy` used by live execution (ADR-003 sink swap).

## Key types

| Type | Project | Role |
|------|---------|------|
| `ITradingStrategy` | Application | `OnBar(BarProcessingContext)` → `OrderIntent` via sink |
| `ITradingStrategyFactory` | Application | Resolves strategy by `TradingVector.TradingLogic` |
| `IBacktestRunner` | Application | Runs simulation over historical bars |
| `SimulationOrderIntentSink` | Infrastructure | Fills, fees, equity curve; one position per vector |
| `Sma200Sma5Strategy` | Infrastructure | Scherman SMA200/SMA5 long/short; no within-vector pyramid |
| `Rsi5ExtremeStrategy` | Infrastructure | RSI(5) cross-up entry; SL = lookback low; TP % (cohort ADR-008) |
| `Rsi5ExtremeSma200` | Infrastructure | Same RSI entry + **SMA200 slope** Direction bias; **no hard SL/TP**; condition exit only |
| `TradingVector` | Kernel | Asset + Direction + TimeFrame + TradingLogic + parameters |
| `VectorInventory` | Kernel | Per-vector tracked qty (hedge-mode accounting) |

## Scherman risk model (`Rsi5ExtremeSma200`)

- **Equal risk** = equal notional per vector (`VectorRiskFraction` × `InitialCapital`; e.g. $100 × 0.05 → $5).
- Worst case (no exit, asset → 0) = lose full notional — not stop-distance sizing.
- **No hard SL** (avoid wick stop-outs); exit on **condition** (`rsiExit` Long ≥ 70; Short ≤ 30).
- **Direction bias**: SMA200 **rising** → Long entries; **falling** → Short entries (slope, not price vs MA).

## Fees

Simulation default **5 bps** per side (Binance USD-M taker 0.05%). Override with `--fee-bps`.

## Verify

Single symbol:

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backtest \
  --market-db .trading-platform-data/market.sqlite --symbol DOGEUSDT \
  --trading-logic Rsi5ExtremeSma200 --direction Long --timeframe 1D \
  --initial-capital 100 --vector-risk 0.05 --fee-bps 5 --rsi-exit 70
```

Full USDM universe (`UsdmTradingUniverse`: TRADING USDT perps, base ≠ USDT/USDC), 1D Long + Short:

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- universe-backtest \
  --market-db .trading-platform-data/market.sqlite \
  --trading-logic Rsi5ExtremeSma200 \
  --initial-capital 100 --vector-risk 0.05 --fee-bps 5
```

Stdout still prints the markdown table; an **HTML report** is also written (default: `.trading-platform-data/reports/universe-{TradingLogic}-{timestamp}.html`). Override with `--html-report <path>`. Open in a browser for sortable PASS/FAIL filters.

Prerequisite: `backfill-1d` (or equivalent) populated `market.sqlite` with 1D candles.

## Do

- Keep strategies **pure bar logic**; emit `OrderIntent` only.
- Share the same strategy class for backtest and live.
- Size each vector with `VectorRiskFraction` × `InitialCapital`.
- Persist simulation runs and backtest verdict JSON for gating live arms.

## Don't

- Place orders or call Binance from Research.
- Add Skender/Binance packages to Domain or Application.
- Pyramid within a single TradingVector.
