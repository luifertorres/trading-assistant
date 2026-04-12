# TradingPlatform (greenfield)

Parallel DDD modular monolith under `src/TradingPlatform`. **No project references** to legacy `TradingAssistant`, `CandlestickData`, or `Backtesting` solutions.

## Build

```bash
dotnet build src/TradingPlatform/TradingPlatform.slnx
```

## Run

**CLI demo** (synthetic bars → per-series SQLite tables → two backtests → analytics rank → portfolio JSON → execution router with logging stub):

```bash
dotnet run --project src/TradingPlatform/src/Tools/TradingPlatform.Cli -- demo
```

**Host** (composition root + broker ACL stub; blocks until cancelled):

```bash
dotnet run --project src/TradingPlatform/src/Hosts/TradingPlatform.Host
```

## Docs

- [ADRs](docs/ADRs.md)
- [Glossary / ubiquitous language](docs/GLOSSARY.md)
- [Design journey — DDD workbook (step-by-step)](docs/design-journey/00-how-to-use-this-trail.md)

## Data

- CLI writes under `./.trading-platform-data/` in the current working directory.
- Host uses `%LocalAppData%/TradingPlatform/` for SQLite and portfolio files.
