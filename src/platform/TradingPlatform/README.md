# TradingPlatform (greenfield)

Parallel DDD modular monolith under `src/platform/TradingPlatform`. **No project references** to legacy `TradingAssistant`, `CandlestickData`, or MVP solutions (`Backtesting`, `WebSocketTrading`).

**Agent instructions:** [AGENTS.md](./AGENTS.md).

## Build

```bash
dotnet build src/platform/TradingPlatform/TradingPlatform.slnx
```

## Tests

```bash
dotnet test src/platform/TradingPlatform/TradingPlatform.slnx
```

- **Unit tests** (domain/kernel invariants): `tests/MarketData.Domain.Tests/`
- **Integration tests** (optional live Binance USD-M REST): `tests/MarketData.Infrastructure.IntegrationTests/` — set `RUN_TRADINGPLATFORM_LIVE_BINANCE=1` to run the network test; see that folder’s `README.md`.
- **CLI backtest tests**: `tests/TradingPlatform.Cli.Tests/`

## Run

**CLI demo** (seed instrument registry → synthetic bars in canonical `candles` table → two backtests → analytics rank → portfolio JSON → execution router with logging stub):

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- demo
```

**CLI: USD-M 1d candle backfill** (Binance.Net rate limits; **schema v2** checkpoint under `--data-root`, default `backfill-1d-checkpoint.json`; use separate `--data-root` values for parallel runs):

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backfill-1d --market-db ./.trading-platform-data/market.sqlite --data-root ./.trading-platform-data --snapshot
```

Optional: `--checkpoint <path>` to override the checkpoint file location.

With `--snapshot`, writes `exchangeInfo-usdm-snapshot-{runId}.json` and `instrument-ids-{runId}.json` (exchange symbol → numeric id) under `--data-root`.

### Greenfield after instrument-registry deploy

Delete the old per-series database and v1 checkpoint, then re-run the backfill (full re-download expected):

```powershell
Remove-Item ./.trading-platform-data/market.sqlite, ./.trading-platform-data/backfill-1d-checkpoint.json -ErrorAction SilentlyContinue
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backfill-1d --market-db ./.trading-platform-data/market.sqlite --data-root ./.trading-platform-data
```

(`-ErrorAction SilentlyContinue` skips missing files if you already deleted one of them.)

**CLI: backtest on persisted Day1 data** (requires prior `backfill-1d` for the target symbol):

```bash
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backtest \
  --market-db ./.trading-platform-data/market.sqlite \
  --symbol BTCUSDT \
  --enter-bar 5 --exit-bar 15
```

| Flag | Required | Default | Description |
|------|----------|---------|-------------|
| `--market-db` | yes | — | SQLite path from `backfill-1d` (instruments + candles) |
| `--research-db` | no | `./.trading-platform-data/research.sqlite` | Simulation run persistence DB |
| `--symbol` | no | `BTCUSDT` | Exchange symbol (must exist in registry) |
| `--from` / `--to` | no | all bars | UTC ISO-8601 open-time filter |
| `--strategy` | no | `FixedWindow` | Strategy kind (only `FixedWindow` today) |
| `--enter-bar` / `--exit-bar` | no | `5` / `15` | 0-based bar indices for `FixedWindow` |
| `--initial-capital` | no | `10000` | Starting capital |
| `--fee-bps` | no | `4` | Fee in basis points per side |
| `--position-fraction` | no | `0.1` | Position notional fraction of initial capital |
| `--save` | no | off | Persist `SimulationRunResult` to research DB |

**Host** (composition root + broker ACL stub; blocks until cancelled):

```bash
dotnet run --project src/platform/TradingPlatform/src/Hosts/TradingPlatform.Host
```

## Docs

- [ADRs](docs/ADRs.md)
- [Glossary / ubiquitous language](docs/GLOSSARY.md)
- [Design journey — DDD workbook](docs/design-journey/00-how-to-use-this-trail.md)

Design journey steps:

| # | Topic |
|---|--------|
| [00](docs/design-journey/00-how-to-use-this-trail.md) | How to use this trail |
| [01](docs/design-journey/01-problem-space-and-outcomes.md) | Problem space and outcomes |
| [02](docs/design-journey/02-event-storming-big-picture.md) | Event storming (big picture) |
| [03](docs/design-journey/03-subdomains-and-bounded-contexts.md) | Subdomains and bounded contexts |
| [04](docs/design-journey/04-context-map.md) | Context map |
| [05](docs/design-journey/05-ubiquitous-language-and-glossary-diff.md) | Ubiquitous language and glossary diff |
| [06](docs/design-journey/06-tactical-ddd-cross-cutting.md) | Tactical DDD cross-cutting |
| [07](docs/design-journey/07-context-market-data.md) | Context: MarketData |
| [08](docs/design-journey/08-context-research.md) | Context: Research |
| [09](docs/design-journey/09-context-analytics.md) | Context: Analytics |
| [10](docs/design-journey/10-context-portfolio.md) | Context: Portfolio |
| [11](docs/design-journey/11-context-execution.md) | Context: Execution |
| [12](docs/design-journey/12-delivery-host-and-cli.md) | Delivery: Host and CLI |
| [13](docs/design-journey/13-end-to-end-alignment-review.md) | End-to-end alignment review |

Repository documentation hub: [docs/README.md](../../../docs/README.md).

## Data

- CLI writes under `./.trading-platform-data/` in the current working directory.
- Host uses `%LocalAppData%/TradingPlatform/` for SQLite and portfolio files.
