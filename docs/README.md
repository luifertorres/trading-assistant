# Documentation

Central index for human-facing documentation in this repository.

## Start here

Automated trading for **Binance Futures (USDT Perpetual)**: real-time market monitoring, technical signals, and order execution with integrated risk management.

Three .NET solutions live under `src/`:

| Bucket | Path | Role |
|--------|------|------|
| **platform** | [`src/platform/TradingPlatform/`](../src/platform/TradingPlatform/) | Greenfield DDD modular monolith (primary development) |
| **legacy** | [`src/legacy/TradingAssistant/`](../src/legacy/TradingAssistant/) | Live bot + CandlestickData (maintenance) |
| **mvp** | [`src/mvp/Backtesting/`](../src/mvp/Backtesting/) | Isolated backtest MVP |

Build, test, and run commands: [`src/README.md`](../src/README.md).

## TradingPlatform

| Doc | Description |
|-----|-------------|
| [README](../src/platform/TradingPlatform/README.md) | Build, CLI commands, data paths |
| [AGENTS.md](../src/platform/TradingPlatform/AGENTS.md) | Agent instructions for Platform work |
| [ADRs](../src/platform/TradingPlatform/docs/ADRs.md) | Architecture decision records |
| [Glossary](../src/platform/TradingPlatform/docs/GLOSSARY.md) | Ubiquitous language and bounded contexts |
| [Design journey](../src/platform/TradingPlatform/docs/design-journey/00-how-to-use-this-trail.md) | DDD workbook (steps 00–13) |

## Legacy live bot

| Doc | Description |
|-----|-------------|
| [Trading Assistant operations](legacy/trading-assistant.md) | Configuration, strategies, Docker, risk management |
| [Host AGENTS.md](../src/legacy/TradingAssistant/TradingAssistant/AGENTS.md) | Legacy host agent instructions |
| Layer AGENTS | [Domain](../src/legacy/TradingAssistant/TradingAssistant.Domain/AGENTS.md), [Application](../src/legacy/TradingAssistant/TradingAssistant.Application/AGENTS.md), [Infrastructure](../src/legacy/TradingAssistant/TradingAssistant.Infrastructure/AGENTS.md) |

## MVP backtesting

[`src/mvp/Backtesting/README.md`](../src/mvp/Backtesting/README.md) — isolated backtest CLI using mock klines and Skender indicators.

## Specifications (OpenSpec)

[`openspec/README.md`](../openspec/README.md) — capability specs, change workflow, Platform vs legacy tracks.

## Archive

Historical planning documents superseded by Platform docs and archived OpenSpec changes: [`archive/README.md`](archive/README.md).

## Agent context

Low-churn routing and principles for AI agents (not end-user docs):

| File | Purpose |
|------|---------|
| [routing-map](../.cursor/context/routing-map.md) | Platform vs legacy routing, OpenSpec bypass matrix |
| [refactor-ledger](../.cursor/context/refactor-ledger.md) | Where new work should go |
| [engineering-principles](../.cursor/context/engineering-principles.md) | Dependency rules, broker boundaries, quality bar |
| [trading-domain](../.cursor/context/trading-domain.md) | Product scope and vocabulary pointers |

Root agent instructions: [`AGENTS.md`](../AGENTS.md).
