# Documentation

Central index for human-facing documentation in this repository.

## Start here

Automated trading for **Binance Futures (USDT Perpetual)**: real-time market monitoring, technical signals, and order execution with integrated risk management.

Three .NET solutions live under `src/`:

| Bucket | Path | Role |
|--------|------|------|
| **platform** | [`src/platform/TradingPlatform/`](../src/platform/TradingPlatform/) | Greenfield DDD modular monolith (primary development) |
| **legacy** | [`src/legacy/TradingAssistant/`](../src/legacy/TradingAssistant/) | Frozen single-project live bot (maintenance) |
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
| [Host AGENTS.md](../src/legacy/TradingAssistant/TradingAssistant/AGENTS.md) | Legacy monolith agent instructions |

## MVP backtesting

[`src/mvp/Backtesting/README.md`](../src/mvp/Backtesting/README.md) — isolated backtest CLI using mock klines and Skender indicators.

## Specifications (OpenSpec)

[`openspec/README.md`](../openspec/README.md) — TradingPlatform capability specs and change workflow.

## Archive

Historical planning documents superseded by Platform docs and archived OpenSpec changes: [`archive/README.md`](archive/README.md).

## Agent context

Low-churn routing and principles for AI agents (not end-user docs):

| File | Purpose |
|------|---------|
| [routing-map](../ai/context/routing-map.md) | Platform vs legacy routing, OpenSpec bypass matrix |
| [refactor-ledger](../ai/context/refactor-ledger.md) | Where new work should go |
| [engineering-principles](../ai/context/engineering-principles.md) | Dependency rules, broker boundaries, quality bar |
| [trading-domain](../ai/context/trading-domain.md) | Product scope and vocabulary pointers |

Root agent instructions: [`AGENTS.md`](../AGENTS.md).
