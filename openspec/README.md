# OpenSpec in this repository

Two tracks share one `openspec/` tree (required by the OpenSpec CLI). **New features belong on TradingPlatform**; legacy specs document the live-bot reference implementation.

## Active track — TradingPlatform

| Main spec capability | Codebase |
|----------------------|----------|
| `trading-platform-marketdata-instrument-registry` | [`src/TradingPlatform/`](../src/TradingPlatform/) MarketData |
| `trading-platform-marketdata-candles-store` | MarketData |
| `trading-platform-marketdata-binance-1d-backfill` | MarketData + Cli |
| `trading-platform-research-backtest-cli` | Research + Cli |

**New changes:** use `/opsx:new` with names prefixed `trading-platform-…`. Implement under `src/TradingPlatform/` only (no references to legacy solutions).

**Shipped changes** live under [`openspec/changes/archive/`](changes/archive/) with date prefixes.

## Legacy reference — TradingAssistant / CandlestickData

| Main spec capability | Codebase |
|----------------------|----------|
| `architecture` | [`src/TradingAssistant/`](../src/TradingAssistant/) |
| `exchange-integration` | TradingAssistant.Infrastructure |
| `market-data` | TradingAssistant + CandlestickData integration |
| `trading-strategies` | TradingAssistant Host strategies |
| `risk-management` | TradingAssistant Host managers |
| `candlestick-data-service` | [`CandlestickData.*`](../src/TradingAssistant/) |
| `candlestick-gap-governance` | CandlestickData |

These specs are **reference only** for real-time multi-strategy trading. Do not extend them for greenfield Platform work; port intent into new `trading-platform-*` capabilities instead.

**Archived legacy proposals** are under [`openspec/changes/archive/`](changes/archive/) with `LEGACY.md` where applicable.

## Naming rules

| Intent | Convention |
|--------|------------|
| Platform feature | `trading-platform-<context>-<feature>` change + matching main spec |
| Legacy reference | Existing capability names above; archived changes keep original names |
| Do not | Create `openspec-legacy/` at repo root (CLI will not discover it) |
| Do not | Apply archived legacy changes to TradingPlatform via `/opsx:apply` |

## Supersession map (legacy → Platform)

| Legacy change / spec area | Platform carry-forward |
|---------------------------|------------------------|
| `backtesting-module` | Research `IBacktestRunner`, simulation models, future `trading-platform-research-*` |
| `multi-broker-support` | Execution broker ACL (future) |
| `event-driven-dca` | Execution event/intent model (future) |
| `risk-management-v2` | Execution / Portfolio risk (future) |
| `clean-architecture-refactor` | Platform DDD slices; only relevant for legacy bot maintenance |
| `cqrs-candle-repository` | Shipped in CandlestickData; Platform uses canonical `candles` store instead |

## Agent routing

- New feature → [`src/TradingPlatform/AGENTS.md`](../src/TradingPlatform/AGENTS.md), then `/opsx:new`
- Legacy bugfix → matching `TradingAssistant/**/AGENTS.md`, read archived change if needed
- Full routing → [`.cursor/context/routing-map.md`](../.cursor/context/routing-map.md), [`.cursor/context/refactor-ledger.md`](../.cursor/context/refactor-ledger.md)

## Workflow commands

| Command | Use when |
|---------|----------|
| `/opsx:new` | Start a Platform change |
| `/opsx:apply` | Implement tasks for a named change |
| `/opsx:verify` | Validate before archive |
| `/opsx:sync` | Merge delta specs into main specs |
| `/opsx:archive` | Move completed change to `changes/archive/` |
