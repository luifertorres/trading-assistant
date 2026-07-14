# Source tree

Documentation index: [docs/README.md](../docs/README.md).

Four .NET solutions live under categorized buckets. See [refactor-ledger](../ai/context/refactor-ledger.md) for where new work should go.

| Bucket | Path | Role | Solution | Docs |
|--------|------|------|----------|------|
| **platform** | [`platform/TradingPlatform/`](platform/TradingPlatform/) | Greenfield DDD modular monolith (primary) | `TradingPlatform.slnx` | [README](platform/TradingPlatform/README.md), [AGENTS](platform/TradingPlatform/AGENTS.md) |
| **legacy** | [`legacy/TradingAssistant/`](legacy/TradingAssistant/) | Frozen single-project live bot (maintenance) | `TradingAssistant.sln` | [AGENTS](legacy/TradingAssistant/TradingAssistant/AGENTS.md) |
| **mvp** | [`mvp/Backtesting/`](mvp/Backtesting/) | Isolated backtest MVP | `Backtesting.sln` | [README](mvp/Backtesting/README.md) |
| **mvp** | [`mvp/WebSocketTrading/`](mvp/WebSocketTrading/) | Live WS kline + SMA short worker | `WebSocketTrading.slnx` | [README](mvp/WebSocketTrading/README.md) |

Agent routing defaults: [AGENTS.md](../AGENTS.md).

## Build

From repo root:

```bash
dotnet build src/platform/TradingPlatform/TradingPlatform.slnx
dotnet build src/legacy/TradingAssistant/TradingAssistant.sln
dotnet build src/mvp/Backtesting/Backtesting.sln
dotnet build src/mvp/WebSocketTrading/WebSocketTrading.slnx
```

## Test

```bash
dotnet test src/platform/TradingPlatform/TradingPlatform.slnx
dotnet test src/mvp/Backtesting/Backtesting.sln
dotnet test src/mvp/WebSocketTrading/WebSocketTrading.Tests/WebSocketTrading.Tests.csproj
```

## Run (common entry points)

**Platform CLI** (demo, backfill, backtest):

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- demo
```

**Legacy live bot:**

```bash
cd src/legacy/TradingAssistant/TradingAssistant
dotnet run
```

**Backtesting MVP:**

```bash
dotnet run --project src/mvp/Backtesting/Backtesting.Mvp.Cli/Backtesting.Mvp.Cli.csproj
```

**WebSocketTrading MVP (live orders — real money):**

Configure [Binance credentials](mvp/WebSocketTrading/README.md#credentials) first, then:

```bash
dotnet run --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

Local `dotnet run` sets `DOTNET_ENVIRONMENT=Development` → **1m** interval (`appsettings.Development.json`). Non-Development runs use the **1D** vector from `appsettings.json`.

## EF migrations (legacy only)

Migrations live inside the single legacy project:

```bash
dotnet ef migrations add <Name> --project src/legacy/TradingAssistant/TradingAssistant
dotnet ef database update --project src/legacy/TradingAssistant/TradingAssistant
```
