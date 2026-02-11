# Trading Assistant - Agent Instructions

## Project Overview

Automated trading bot for **Binance Futures (USDT Perpetual)** built with .NET 10 and C# 13. Monitors the market in real-time, detects technical signals, and executes trades with integrated risk management.

## Architecture

Modern simplified **Clean Architecture** with **DDD** — no Ports and Adapters pattern.

### Layers (inner to outer)

| Layer | Project | Purpose |
|-------|---------|---------|
| Domain | `TradingAssistant.Domain` | Entities, Value Objects, domain logic. Zero external dependencies. |
| Application | `TradingAssistant.Application` | Interfaces, MediatR notifications/events, orchestration. |
| Infrastructure | `TradingAssistant.Infrastructure` | Adapters (Binance, EF Core, FASTER), repository implementations. |
| Host | `TradingAssistant` | DI composition root, BackgroundServices, configuration. |

### Dependency Rule

```
Host → Infrastructure → Application → Domain
```

**Never** add a reverse dependency. Domain and Application must remain infrastructure-agnostic.

## Key Conventions

- **Rich Domain Model**: Entities have behavior, not just data (no anemic models).
- **Broker-agnostic domain**: The domain must not reference any specific broker library (Binance.Net, etc.). All broker specifics live in Infrastructure behind abstractions.
- **MediatR**: Used for in-process messaging (notifications, requests). Registered in Application; host assembly also scans for handlers.
- **Value Objects**: Use `record` or `readonly struct` for immutability.
- **High-performance types**: Use `struct` for hot-path types like `Candle`.
- **Extension methods**: Preferred for conversions and utility operations.
- **Naming**: PascalCase for public members, `_camelCase` for private fields, no Hungarian notation.
- **Async/Await**: Use `CancellationToken` in all async signatures.

## Solution Structure

```
src/TradingAssistant/
├── TradingAssistant.sln
├── TradingAssistant.Domain/         → See Domain/AGENTS.md
├── TradingAssistant.Application/    → See Application/AGENTS.md
├── TradingAssistant.Infrastructure/ → See Infrastructure/AGENTS.md
└── TradingAssistant/                → See Host/AGENTS.md
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10.0 |
| Exchange API | Binance.Net 12.3.1 |
| Mediator/CQRS | MediatR 14.0 |
| Indicators | Skender.Stock.Indicators 2.7.1 |
| In-memory cache | Microsoft FASTER (FasterKV) |
| Database | SQLite via EF Core 10.0.2 |
| Notifications | Telegram |

## Migrations Policy

Never edit migration files manually. Always run from `src/TradingAssistant/`:

```bash
dotnet ef migrations add <Name> --project TradingAssistant.Infrastructure --startup-project TradingAssistant --output-dir Migrations
dotnet ef database update --project TradingAssistant.Infrastructure --startup-project TradingAssistant
```

## Current Refactoring Status

The project is mid-refactor toward full Clean Architecture. Remaining work:
1. Move handlers/strategies from Host to Application layer.
2. Remove `Binance.Net` dependency from Domain (replace with own enums: `TimeFrame`, `OrderSide`, etc.).
3. Replace direct `BinanceService` usage with `IExchangeService` everywhere.

## OpenSpec

This project uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven development.

- **Main specs** (current system capabilities): `openspec/specs/<capability>/spec.md`
- **Changes** (planned features): `openspec/changes/<change-name>/` with artifacts: proposal, specs, design, tasks

Use `/opsx:apply <change-name>` to implement a change, or `/opsx:new` to start a new one.
