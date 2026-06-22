# Execution — Agent Instructions

## Purpose

Route portfolio vectors to a live order sink; Binance USD-M adapter with real-money guardrails.

## Key types

| Type | Project | Role |
|------|---------|------|
| `ILiveOrderIntentSink` | Application | Accepts `OrderIntent` for live placement |
| `PortfolioExecutionRouter` | Application | Same strategy factory as Research; live sink |
| `LiveTradingOptions` | Infrastructure | Armed, caps, kill-switch path |
| `BinanceLiveOrderIntentSink` | Infrastructure | Market entry + StopMarket SL; 1x ISOLATED |
| `LoggingLiveOrderIntentSink` | Infrastructure | Stub for disarmed / dev |

## Do

- Enforce [.cursor/rules/live-trading-safety.mdc](../../../../.cursor/rules/live-trading-safety.mdc) in every order path.
- Size via `MARKET_LOT_SIZE`; reject over-cap notional.
- Require PASS backtest verdict + portfolio membership before arming.

## Don't

- Arm by default or auto-scale size.
- Skip leverage/margin setup before entry.

## Verify

```bash
dotnet build src/platform/TradingPlatform/TradingPlatform.slnx
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- fire-test-order --symbol DOGEUSDT --market-db .trading-platform-data/market.sqlite
# (fails disarmed — expected). With --arm only after PASS verdict + user confirmation.
```
