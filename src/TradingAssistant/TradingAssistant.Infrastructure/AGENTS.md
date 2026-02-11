# Infrastructure Layer - Agent Instructions

## Purpose

Implements the abstractions defined in Application and Domain. Contains all external integrations, data persistence, and adapter logic. This is the **only layer** that should reference third-party infrastructure packages.

## Rules

### Dependency Constraints

- **MAY** reference `TradingAssistant.Application` and `TradingAssistant.Domain`.
- **NEVER** reference the Host project.
- This is where all infrastructure NuGet packages belong (Binance.Net, EF Core, FASTER, etc.).

### Adapter Pattern

- Every external integration must implement an Application-layer interface.
- Adapters translate between infrastructure-specific types and domain types.
- Example: `BinanceExchangeService` implements `IExchangeService`, translating Binance DTOs to domain entities.

### Binance Integration (`Binance/`)

| File | Purpose |
|------|---------|
| `BinanceService.cs` | Core Binance WebSocket/REST service — manages connections, candle streaming, indicator calculation. |
| `BinanceExchangeService.cs` | Adapter: implements `IExchangeService` using `BinanceService` internally. |
| `BinanceExtensions.cs` | Extension methods for Binance-specific conversions. |
| `BinanceServiceNotConfiguredException.cs` | Custom exception for missing configuration. |

### Data Persistence

**EF Core (SQLite):**
- `TradingContext.cs` — DbContext with `OpenPositions` DbSet.
- `TradingContextFactory.cs` — Design-time factory for EF migrations.
- Database location: `%LocalApplicationData%/trading.db`
- Migrations in `Migrations/` folder — **never edit manually**.

**FASTER (In-memory cache) (`Faster/`):**
- `FasterCandleRepository.cs` — Implements `ICandleRepository` using FasterKV for high-performance candle storage.
- `CandleIdSerializer.cs` / `CandleSerializer.cs` — Custom serializers for FASTER.
- Log location: `c:/temp/hlog.log` (needs to be made configurable).

### Queue

- `TradingSignalQueue.cs` — Implements `ITradingSignalQueue` using `Channel<T>` for in-process async signal queuing.

### DI Registration

- `ServiceCollectionExtensions.AddInfrastructure(IConfiguration)` — registers all infrastructure services.
- `ServiceCollectionExtensions.AddBinance(Action<BinanceRestOptions>)` — registers Binance-specific services.

## Conventions

- Group files by integration in subdirectories (`Binance/`, `Faster/`).
- New integrations get their own subdirectory (e.g., future `CapitalCom/` adapter).
- All adapters must be registered in the DI extension methods.
- Use `sealed` classes for adapter implementations.
- `Class1.cs` is a placeholder — delete it when adding new content.
