# TradingPlatform (greenfield)

Parallel DDD modular monolith under `src/TradingPlatform`. **No project references** to legacy `TradingAssistant`, `CandlestickData`, or `Backtesting` solutions.

**Agent instructions:** [AGENTS.md](./AGENTS.md).

## Build

```bash
dotnet build src/TradingPlatform/TradingPlatform.slnx
```

## Tests

```bash
dotnet test src/TradingPlatform/TradingPlatform.slnx
```

- **Unit tests** (domain/kernel invariants): `tests/MarketData.Domain.Tests/`
- **Integration tests** (optional live Binance USD-M REST): `tests/MarketData.Infrastructure.IntegrationTests/` — set `RUN_TRADINGPLATFORM_LIVE_BINANCE=1` to run the network test; see that folder’s `README.md`.

## Run

**CLI demo** (seed instrument registry → synthetic bars in canonical `candles` table → two backtests → analytics rank → portfolio JSON → execution router with logging stub):

```bash
dotnet run --project src/TradingPlatform/src/Tools/TradingPlatform.Cli -- demo
```

**CLI: USD-M 1d candle backfill** (Binance.Net rate limits; **schema v2** checkpoint under `--data-root`, default `backfill-1d-checkpoint.json`; use separate `--data-root` values for parallel runs):

```bash
dotnet run --project src/TradingPlatform/src/Tools/TradingPlatform.Cli -- backfill-1d --market-db ./.trading-platform-data/market.sqlite --data-root ./.trading-platform-data --snapshot
```

Optional: `--checkpoint <path>` to override the checkpoint file location.

With `--snapshot`, writes `exchangeInfo-usdm-snapshot-{runId}.json` and `instrument-ids-{runId}.json` (exchange symbol → numeric id) under `--data-root`.

### Greenfield after instrument-registry deploy

Delete the old per-series database and v1 checkpoint, then re-run the backfill (full re-download expected):

```powershell
Remove-Item ./.trading-platform-data/market.sqlite, ./.trading-platform-data/backfill-1d-checkpoint.json -ErrorAction SilentlyContinue
dotnet run --project src/TradingPlatform/src/Tools/TradingPlatform.Cli -- backfill-1d --market-db ./.trading-platform-data/market.sqlite --data-root ./.trading-platform-data
```

(`-ErrorAction SilentlyContinue` skips missing files if you already deleted one of them.)

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
