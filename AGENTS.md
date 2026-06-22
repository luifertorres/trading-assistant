# Trading Assistant - Agent Instructions

## Project Overview

Automated trading bot for **Binance Futures (USDT Perpetual)** built with .NET 10 and C# 13. Monitors the market in real-time, detects technical signals, and executes trades with integrated risk management.

Human documentation: [docs/README.md](docs/README.md).

## Repository routing (read when ambiguous)

**Default for new work:** [`src/platform/TradingPlatform/`](src/platform/TradingPlatform/) — modular monolith by bounded context; **no references** to legacy solutions. Start with [`src/platform/TradingPlatform/AGENTS.md`](src/platform/TradingPlatform/AGENTS.md) and [`src/platform/TradingPlatform/README.md`](src/platform/TradingPlatform/README.md).

**Legacy reference:** [`src/legacy/TradingAssistant/`](src/legacy/TradingAssistant/) — frozen **single-project** live bot (maintenance only). Avoid growing it when the same capability belongs in TradingPlatform (see [`.cursor/context/refactor-ledger.md`](.cursor/context/refactor-ledger.md)).

**MVP tooling:** [`src/mvp/Backtesting/`](src/mvp/Backtesting/) — isolated backtest CLI.

**Source tree index:** [`src/README.md`](src/README.md).

**Routing protocol:** infer **intent and blast radius**, not keywords. If the task is cross-cutting, multi-context, or unclear on OpenSpec vs direct implementation, read `[.cursor/context/routing-map.md](.cursor/context/routing-map.md)` first. For a short preflight only, use the **chief-of-staff** skill (`.cursor/skills/chief-of-staff/SKILL.md`).

### Modular context (by rate of change)


| File                                                                                     | Purpose                                                                               |
| ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `[.cursor/context/routing-map.md](.cursor/context/routing-map.md)`                       | Paired-change hints, OpenSpec **bypass** command matrix, Platform vs legacy defaults. |
| `[.cursor/context/engineering-principles.md](.cursor/context/engineering-principles.md)` | Dependency direction, broker boundaries, migrations/testing bar.                      |
| `[.cursor/context/trading-domain.md](.cursor/context/trading-domain.md)`                 | Product scope, vocabulary pointers, operational risk stance.                          |
| `[.cursor/context/refactor-ledger.md](.cursor/context/refactor-ledger.md)`               | Migration story: what is greenfield vs frozen legacy.                                 |
| `[.cursor/context/routing-overrides.md](.cursor/context/routing-overrides.md)`           | Log routing corrections; promote patterns after three similar overrides.              |
| `[.cursor/context/delivery-principles.md](.cursor/context/delivery-principles.md)`     | Same-session wins, ship-a-slice ritual, token-lean scoped context.                    |


## Architecture

### TradingPlatform (greenfield)

Modular monolith with **DDD bounded contexts** — MarketData, Research, Analytics, Portfolio, Execution, Kernel, Host, Cli. See [`src/platform/TradingPlatform/AGENTS.md`](src/platform/TradingPlatform/AGENTS.md).

### Legacy (reference)

Single **Worker Service** project under `src/legacy/TradingAssistant/TradingAssistant/` — strategies, Binance integration, EF Core, and FASTER in one assembly. Not layered; not extended for new features.

### MVP

Isolated backtest CLI under `src/mvp/Backtesting/`.

## Key Conventions (Platform)

- **Rich Domain Model**: Entities have behavior, not just data (no anemic models).
- **Binance.Net as the Binance framework**: For .NET code that talks to Binance (REST, WebSocket, USD-M models), use **Binance.Net** as the supported stack. **Platform Domain and Application** stay broker-agnostic in public types; Infrastructure adapters own Binance.Net. Legacy monolith and MVP may use Binance.Net directly. See **Binance.Net framework** below.
- **MediatR**: Used for in-process messaging in Platform Application and legacy monolith.
- **Value Objects**: Use `record` or `readonly struct` for immutability.
- **High-performance types**: Use `struct` for hot-path types like `Candle`.
- **Naming**: PascalCase for public members, `_camelCase` for private fields.
- **Async/Await**: Use `CancellationToken` in all async signatures.

## Binance.Net framework (.NET)

Treat **[Binance.Net](https://github.com/JKorf/Binance.Net)** as the **framework** for Binance-facing .NET code: `IBinanceRestClient` / `IBinanceSocketClient`, USD-M APIs under `UsdFuturesApi`, and **models as returned by the API**. Prefer library types over re-modeling exchange payloads unless translating into Platform Domain or Application interfaces.

**Where it applies:** Platform Infrastructure (MarketData, Execution), legacy monolith (`BinanceService`), and **Backtesting MVP**.

**Practices:** Keep **Binance.Net package versions aligned** across projects (12.11.x). Legacy uses `BinanceCredentials` for API keys (Binance.Net 12.11+).

Cursor rule: `.cursor/rules/binance-net.mdc`.

## Solution Structure

See [src/README.md](src/README.md) for build/run commands. Buckets:

```
src/
├── platform/TradingPlatform/
│   ├── TradingPlatform.slnx         → Greenfield DDD modular monolith
│   └── src/                         → Bounded contexts, Kernel, Host, Cli
│
├── legacy/TradingAssistant/
│   ├── TradingAssistant.sln
│   └── TradingAssistant/            → Single-project live bot (see AGENTS.md)
│
└── mvp/Backtesting/
    ├── Backtesting.sln
    └── …                            → See Backtesting/README.md
```

## Technology Stack


| Component       | Technology                                                 |
| --------------- | ---------------------------------------------------------- |
| Runtime         | .NET 10.0                                                  |
| Exchange API    | Binance.Net 12.11.x (keep versions aligned across projects) |
| Mediator/CQRS   | MediatR 14.0                                               |
| Indicators      | Skender.Stock.Indicators 2.7.1                             |
| In-memory cache | Microsoft FASTER (FasterKV) — legacy monolith              |
| Database        | SQLite via EF Core 10.0.2                                  |
| Notifications   | Telegram — legacy monolith                                 |


## Migrations Policy (legacy only)

Never edit migration files manually:

```bash
dotnet ef migrations add <Name> --project src/legacy/TradingAssistant/TradingAssistant
dotnet ef database update --project src/legacy/TradingAssistant/TradingAssistant
```

## OpenSpec

This project uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for **TradingPlatform** spec-driven development. See [openspec/README.md](openspec/README.md).

- **Main specs:** `openspec/specs/trading-platform-*`
- **Changes:** `openspec/changes/<change-name>/` with artifacts: proposal, specs, design, tasks

Use `/opsx:apply <change-name>` to implement a change, or `/opsx:new` to start a new one.
