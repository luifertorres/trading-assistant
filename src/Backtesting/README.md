# Backtesting MVP (`Backtesting.Mvp`)

Isolated backtest loop using **mock `IBinanceKline`** data, **Skender.Stock.Indicators** RSI, and a small **CLI** for quick runs. No dependency on the main trading host.

**Solution:** [`Backtesting.sln`](Backtesting.sln) in this folder contains the three Backtesting projects (separate from [`TradingAssistant.sln`](../TradingAssistant/TradingAssistant.sln)).

## Build / test (solution)

From repo root:

```bash
dotnet build src/Backtesting/Backtesting.sln
dotnet test src/Backtesting/Backtesting.sln
```

## Run

From repo root (or any path):

```bash
dotnet run --project src/Backtesting/Backtesting.Mvp.Cli/Backtesting.Mvp.Cli.csproj
```

Optional equity CSV:

```bash
dotnet run --project src/Backtesting/Backtesting.Mvp.Cli/Backtesting.Mvp.Cli.csproj -- --csv equity.csv
```

## Tests (project only)

```bash
dotnet test src/Backtesting/Backtesting.Mvp.Tests/Backtesting.Mvp.Tests.csproj
```

## References

- [Binance USDⓈ-M Futures — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info) (REST ordering, timestamps, limits)
- [Binance.Net (JKorf)](https://github.com/JKorf/Binance.Net) — `IBinanceKline`, clients, enums
- In-repo patterns: `Rsi5RealtimeIndicatorTracker`, `CandleExtensions` under `src/TradingAssistant/TradingAssistant/` (quotes from klines)

## Strategy (MVP)

Long-only: RSI(14) with **entry** when RSI crosses **up through 30** and **exit** when RSI crosses **up through 70**. Market fill at bar **close**; fees in **basis points per side** on notional.
