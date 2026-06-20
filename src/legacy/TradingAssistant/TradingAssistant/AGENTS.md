# Host Layer - Agent Instructions

## Purpose

Composition root and entry point. This is a **Worker Service** that wires up DI, configures services, and runs background workers. It should contain minimal business logic — only DI registration and BackgroundService orchestration.

## Rules

### Dependency Constraints

- **MAY** reference `TradingAssistant.Application` and `TradingAssistant.Infrastructure`.
- **NEVER** reference `TradingAssistant.Domain` directly for business logic (go through Application interfaces).
- Infrastructure types are only used here for DI registration, not for business logic.

### Binance

- Binance connectivity is implemented with **Binance.Net** in Infrastructure; the host wires `AddBinance()` and should prefer **`IExchangeService`** in new code over leaking `BinanceService` details into workers.

### Program.cs (Composition Root)

- `AddApplication()` — registers MediatR from the Application assembly.
- `AddMediatR()` — also scans the Host assembly for handlers (temporary, until handlers move to Application).
- `AddBinance()` — configures Binance API credentials.
- `AddInfrastructure()` — registers all infrastructure services.
- Hosted services are registered individually via `AddHostedService<T>()`.

### Strategies (pending migration to Application)

Strategies implement `INotificationHandler<SmasAndRsisCalculatedEvent>`:

| Strategy | Description |
|----------|-------------|
| `MeanReversion1mStrategy` | Mean reversion on 1-minute candles |
| `MeanReversion1mOr15mStrategy` | Mean reversion on 1m or 15m candles |
| `MeanReversion5mStrategy` | Mean reversion on 5-minute candles |
| `TrendFollowing1mOr15mStrategy` | Trend following on 1m/15m candles |
| `Rsi5Below10On1mStrategy` | RSI(5) < 10 on 1-minute candles |
| `Rsi5Below10On1dStrategy` | RSI(5) < 10 on daily candles |
| `Rsi5ExtremeStrategy` | RSI(5) extreme levels (IRequestHandler) |

### Workers (BackgroundServices)

| Worker | Purpose |
|--------|---------|
| `TradingSignalWorker` | Dequeues and processes trading signals |
| `PositionWriterWorker` | Persists open positions to SQLite |
| `Rsi5RealtimeIndicatorWorker` | Real-time RSI(5) calculation |
| `Ema5ClosePositionWorker` | EMA(5)-based position closing |
| `Rsi200ClosePositionWorker` | RSI(200)-based position closing |
| `BreakEvenWorker` | Break-even management (currently disabled) |

### Managers (BackgroundServices)

| Manager | Purpose |
|---------|---------|
| `StopLossManager` | Monitors and triggers stop-loss orders |
| `TakeProfitManager` | Monitors and triggers take-profit orders (currently disabled) |
| `TrailingStopManager` | Dynamic trailing stop management (currently disabled) |
| `SteppedTrailingStopManager` | Stepped trailing stop management (currently disabled) |

### Handlers

| Handler | Handles | Purpose |
|---------|---------|---------|
| `TradeHandler` | `TradeRequest` | Executes trades via exchange |
| `ClosePositionHandler` | `ClosePositionRequest` | Closes positions |
| `RsiCandleClosedHandler` | `CandleClosedNotification` | RSI calculation on candle close |
| `TradingSignalHandler` | `TradingSignalNotification` | Processes trading signals |
| `RsiIndicatorConditionDispatcher` | — | Dispatches indicator threshold events |

### Configuration

Configuration is in `appsettings.json` with environment overrides in `appsettings.Development.json`:

```
Binance:Futures:ApiKey / ApiSecret   → API credentials (use User Secrets in dev)
Binance:Service:TimeFrameSeconds     → Candle timeframe
Binance:Service:CandlestickSize      → Historical candle count
Binance:Strategy:LengthA-D           → Indicator lengths
Binance:RiskManagement:*             → Stop-loss, take-profit, trailing stop settings
Binance:Indicators:*                 → Per-timeframe indicator lengths
```

## Conventions

- New workers/managers: register in `Program.cs` via `AddHostedService<T>()`.
- Disabled services: comment out the registration line (do not delete).
- Logging: use `ILogger<T>` injected via constructor. Telegram logging is configured for production.
