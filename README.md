# Trading Assistant

Automated trading bot for **Binance Futures (USDT Perpetual)**. Monitors the market in real-time, detects technical signals, and executes trades with integrated risk management.

Three .NET solutions live under `src/`:

| Bucket | Solution | Role |
|--------|----------|------|
| `platform/` | TradingPlatform | DDD modular monolith (primary development) |
| `legacy/` | TradingAssistant | Frozen single-project live bot (maintenance) |
| `mvp/` | Backtesting | Isolated backtest MVP |

**Full documentation:** [docs/README.md](docs/README.md)

**Agent / editor setup after clone:** [EDITOR-AGENTS.md](EDITOR-AGENTS.md)

## Quick start

Build all solutions from the repo root:

```bash
dotnet build src/platform/TradingPlatform/TradingPlatform.slnx
dotnet build src/legacy/TradingAssistant/TradingAssistant.sln
dotnet build src/mvp/Backtesting/Backtesting.sln
```

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

More commands (test, EF migrations): [src/README.md](src/README.md).

## Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10.0 / C# 13 |
| Exchange API | Binance.Net 12.11.x |
| Mediator | MediatR 14.0 |
| Indicators | Skender.Stock.Indicators 2.7.1 |
| Cache | Microsoft FASTER |
| Database | SQLite (EF Core 10.0.2) |

## Warning

This software is for educational and experimental use. Leveraged cryptocurrency trading carries a high risk of loss. Use at your own responsibility.

## License

See [LICENSE](LICENSE).
