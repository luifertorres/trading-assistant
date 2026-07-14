---
name: binance-net
description: Use Binance.Net as the .NET framework for Binance APIs in this repo. Use when adding or changing USD-M REST/WebSocket code, exchange info, klines, filters, or MVP Binance usage (Backtesting, WebSocketTrading).
---

# Binance.Net in trading-assistant

## Stance

**Binance.Net** is the supported **framework** for Binance in .NET here: use `IBinanceRestClient` / `IBinanceSocketClient`, `UsdFuturesApi`, and **API-shaped types** from the package (e.g. `BinanceFuturesUsdtSymbol`, filters, klines). Do not invent parallel exchange DTOs unless you are mapping **into Domain** or **through Application interfaces** (use domain types at those boundaries).

## Where to look

- Root [AGENTS.md](../../AGENTS.md) — **Binance.Net framework (.NET)** section, stack table, layer rules.
- [TradingAssistant AGENTS.md](../../src/legacy/TradingAssistant/TradingAssistant/AGENTS.md) — legacy monolith Binance usage.
- [Backtesting README](../../src/mvp/Backtesting/README.md) — isolated backtest MVP using Binance.Net directly.
- [WebSocketTrading README](../../src/mvp/WebSocketTrading/README.md) — live Worker; WS market streams + WS API orders (Binance.Net **13.1.1**).
- `ai/templates/rules/binance-net.mdc` — same boundaries (`/ai-onboard` copies to `.cursor/rules/`).

## Practices

- Align **package version** across Platform and legacy (12.11.x). **WebSocketTrading MVP** pins 13.1.1 independently — do not bump Platform/legacy when changing that solution.
- Follow existing patterns in `BinanceService` (e.g. `GetExchangeInfoAsync`, `GetResultOrError`).
- Infrastructure implements exchange ports; Host and strategies should prefer those over raw `BinanceService` where refactor allows.
