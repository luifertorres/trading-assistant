---
name: binance-net
description: Use Binance.Net as the .NET framework for Binance APIs in this repo. Use when adding or changing USD-M REST/WebSocket code, exchange info, klines, filters, or Backtesting MVP Binance usage.
---

# Binance.Net in trading-assistant

## Stance

**Binance.Net** is the supported **framework** for Binance in .NET here: use `IBinanceRestClient` / `IBinanceSocketClient`, `UsdFuturesApi`, and **API-shaped types** from the package (e.g. `BinanceFuturesUsdtSymbol`, filters, klines). Do not invent parallel exchange DTOs unless you are mapping **into Domain** or **through Application interfaces** (use domain types at those boundaries).

## Where to look

- Root [AGENTS.md](mdc:AGENTS.md) — **Binance.Net framework (.NET)** section, stack table, layer rules.
- [Infrastructure AGENTS.md](mdc:src/TradingAssistant/TradingAssistant.Infrastructure/AGENTS.md) — `Binance/` layout, framework stance, adapters.
- [Backtesting README](mdc:src/Backtesting/README.md) — isolated MVP using Binance.Net directly.
- `.cursor/rules/binance-net.mdc` — same boundaries with path globs for Cursor.

## Practices

- Align **package version** across projects that reference Binance.Net.
- Follow existing patterns in `BinanceService` (e.g. `GetExchangeInfoAsync`, `GetResultOrError`).
- Infrastructure implements `IExchangeService`; Host and strategies should prefer that over raw `BinanceService` where refactor allows.
