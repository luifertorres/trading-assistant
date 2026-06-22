# Legacy TradingAssistant — Agent Instructions

## Purpose

Single-project **Worker Service** live bot for Binance USD-M Futures. All code lives in this project — strategies, workers, managers, `BinanceService`, EF Core, and FASTER cache. **Maintenance only**; new features belong in [`src/platform/TradingPlatform/`](../../../../platform/TradingPlatform/).

## Layout

```
src/legacy/TradingAssistant/
├── TradingAssistant.sln
└── TradingAssistant/          ← this project (everything)
    ├── Program.cs             ← composition root
    ├── BinanceService.cs
    ├── *Strategy.cs, *Worker.cs, *Manager.cs, *Handler.cs
    ├── TradingContext.cs + Migrations/
    └── appsettings*.json
```

## Binance

Use **Binance.Net** directly (`AddBinance`, `BinanceCredentials`, `IBinanceRestClient` / `IBinanceSocketClient`). Keep package version aligned with Platform and MVP (12.11.x).

## Conventions

- **MediatR**: notifications and requests; handlers in this assembly.
- **BackgroundService**: workers and risk managers; register in `Program.cs` via `AddHostedService<T>()`.
- **EF Core**: migrations live in `Migrations/` inside this project.
- Disabled services: comment out registration in `Program.cs` (do not delete).

## EF migrations

From repo root:

```bash
dotnet ef migrations add <Name> --project src/legacy/TradingAssistant/TradingAssistant
dotnet ef database update --project src/legacy/TradingAssistant/TradingAssistant
```

Never edit migration files manually.

## Porting to Platform

See [`legacy-port-map.md`](../../../../platform/TradingPlatform/docs/legacy-port-map.md) for behavior → bounded context mapping.
