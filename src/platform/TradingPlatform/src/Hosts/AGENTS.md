# Hosts — Agent Instructions

## Purpose

Composition root for long-running TradingPlatform: DI wiring, configuration, background workers.

## Projects

| Project | Role |
|---------|------|
| `TradingPlatform.Host` | Worker host: live feed + strategy evaluation on closed candles |
| `TradingPlatform.Cli` | One-shot commands: backfill, backtest, cohort-compose, fire-test-order |

## Key workers

| Worker | Role |
|--------|------|
| `LiveStrategyWorker` | Subscribes to `ILiveCandleFeed`; evaluates `Rsi5ExtremeStrategy` on closed 4H bars; replays last closed candle at startup |

## Do

- Keep `Program.cs` thin — register extensions from each context's `ServiceCollectionExtensions`.
- Store data under `%LocalAppData%/TradingPlatform` for Host; Cli may use `.trading-platform-data` in cwd.

## Don't

- Put strategy logic in the host; delegate to Research + Execution.

## Verify

```bash
dotnet run --project src/platform/TradingPlatform/src/Hosts/TradingPlatform.Host
# Expect log: composition ready + live feed subscribed (when portfolio configured)
```
