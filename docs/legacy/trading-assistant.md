# Legacy Trading Assistant — operations guide

Operational guide for the **legacy live bot** under `src/legacy/TradingAssistant/`. For agent routing, see [AGENTS.md](../../src/legacy/TradingAssistant/TradingAssistant/AGENTS.md). For porting behavior to Platform, see [legacy-port-map.md](../../src/platform/TradingPlatform/docs/legacy-port-map.md).

## Features

- Real-time candlestick monitoring via WebSocket
- Technical indicators (RSI, SMAs)
- Configurable trading strategies
- Automatic order execution (Market/Limit)
- Risk management: Stop-Loss, Take-Profit, Break-Even, Trailing Stop
- Telegram notifications
- High-performance in-memory cache (Microsoft FASTER)

## Stack

| Component | Technology |
|-----------|------------|
| Language | C# / .NET 10.0 |
| Application type | Worker Service (single project) |
| Exchange API | Binance.Net 12.11.x (REST + WebSockets) |
| Mediator/CQRS | MediatR 14.0 |
| Technical indicators | Skender.Stock.Indicators 2.7.1 |
| In-memory cache | Microsoft FASTER (FasterKV) |
| Database | SQLite (EF Core 10.0.2) |
| Notifications | Telegram |
| Container | Docker (Linux) |

## Architecture

Single-project monolith — all code in `TradingAssistant/`:

```
src/legacy/TradingAssistant/
├── TradingAssistant.sln
└── TradingAssistant/
    ├── Program.cs
    ├── BinanceService.cs
    ├── *Strategy.cs, *Worker.cs, *Manager.cs
    ├── TradingContext.cs + Migrations/
    └── appsettings*.json
```

## Requirements

- .NET 10.0 SDK
- Binance account with Futures API key enabled
- (Optional) Telegram bot for notifications

## Configuration

### API keys

User Secrets (recommended for development):

```bash
cd src/legacy/TradingAssistant/TradingAssistant
dotnet user-secrets set "Binance:Futures:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "Binance:Futures:ApiSecret" "YOUR_API_SECRET"
```

Or edit `appsettings.Development.json`:

```json
{
  "Binance": {
    "Futures": {
      "ApiKey": "YOUR_API_KEY",
      "ApiSecret": "YOUR_API_SECRET"
    }
  }
}
```

### Telegram (optional)

```json
{
  "Logging": {
    "Telegram": {
      "AccessToken": "YOUR_BOT_TOKEN",
      "ChatId": "YOUR_CHAT_ID"
    }
  }
}
```

### Strategy and risk

In `appsettings.json`:

```json
{
  "Binance": {
    "Service": {
      "TimeFrameSeconds": 3600,
      "CandlestickSize": 2200
    },
    "Strategy": {
      "LengthA": 5,
      "LengthB": 8,
      "LengthC": 20,
      "LengthD": 200
    },
    "RiskManagement": {
      "AccountMarginPercentage": 0.5,
      "StopLossRoi": 100,
      "MinRoiBeforeBreakEven": 100,
      "TakeProfitRoi": 100
    }
  }
}
```

`TimeFrameSeconds`: 1H = 3600, 5m = 300, 1m = 60.

## Run

### Local development

```bash
cd src/legacy/TradingAssistant/TradingAssistant
dotnet run
```

### Docker

```bash
docker build -t trading-assistant \
  -f src/legacy/TradingAssistant/TradingAssistant/Dockerfile \
  src/legacy/TradingAssistant

docker run -d \
  -e Binance__Futures__ApiKey=YOUR_API_KEY \
  -e Binance__Futures__ApiSecret=YOUR_API_SECRET \
  trading-assistant
```

## Strategies

| Strategy | Description |
|----------|-------------|
| `MeanReversion1mOr15mStrategy` | Mean reversion on short timeframes |
| `MeanReversion5mStrategy` | Mean reversion on 5-minute bars |
| `TrendFollowing1mOr15mStrategy` | Trend following |

## Risk management

- **Stop-Loss** — automatic close on maximum loss
- **Take-Profit** — automatic close at target
- **Break-Even** — move stop-loss to entry when in profit
- **Trailing Stop** — dynamic stop-loss following price
- **Stepped Trailing Stop** — trailing stop with ROI steps

## EF migrations

From repo root:

```bash
dotnet ef migrations add <Name> --project src/legacy/TradingAssistant/TradingAssistant
dotnet ef database update --project src/legacy/TradingAssistant/TradingAssistant
```

Never edit migration files manually.

## Related documentation

- [Documentation index](../README.md)
- [Source tree build/run](../../src/README.md)
- [Legacy port map](../../src/platform/TradingPlatform/docs/legacy-port-map.md)

## Warning

This software is for educational and experimental use. Leveraged cryptocurrency trading carries a high risk of loss. Use at your own responsibility.
