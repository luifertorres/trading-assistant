# OpenSpec in this repository

OpenSpec documents **TradingPlatform** capabilities under `src/platform/TradingPlatform/`. Legacy live-bot code under `src/legacy/TradingAssistant/` is a frozen single-project reference with no OpenSpec track.

## Main specs — TradingPlatform

| Capability | Codebase |
|------------|----------|
| `trading-platform-marketdata-instrument-registry` | MarketData |
| `trading-platform-marketdata-candles-store` | MarketData |
| `trading-platform-marketdata-binance-1d-backfill` | MarketData + Cli |
| `trading-platform-research-backtest-cli` | Research + Cli |

**New changes:** use `/opsx:new` with names prefixed `trading-platform-…`. Implement under `src/platform/TradingPlatform/` only (no references to legacy solutions).

**Shipped changes** live under [`openspec/changes/archive/`](changes/archive/) with date prefixes.

## Naming rules

| Intent | Convention |
|--------|------------|
| Platform feature | `trading-platform-<context>-<feature>` change + matching main spec |
| Do not | Create `openspec-legacy/` at repo root (CLI will not discover it) |

## Agent routing

- New feature → [`src/platform/TradingPlatform/AGENTS.md`](../src/platform/TradingPlatform/AGENTS.md), then `/opsx:new`
- Legacy bugfix → [`src/legacy/TradingAssistant/TradingAssistant/AGENTS.md`](../src/legacy/TradingAssistant/TradingAssistant/AGENTS.md); port map at [`legacy-port-map.md`](../src/platform/TradingPlatform/docs/legacy-port-map.md)
- Editor bootstrap → [`EDITOR-AGENTS.md`](../EDITOR-AGENTS.md)
- OpenSpec CLI setup → [`SETUP.md`](SETUP.md)
- Full routing → [`.cursor/context/routing-map.md`](../.cursor/context/routing-map.md), [`.cursor/context/refactor-ledger.md`](../.cursor/context/refactor-ledger.md)

## Workflow commands

| Command | Use when |
|---------|----------|
| `/opsx:new` | Start a Platform change |
| `/opsx:apply` | Implement tasks for a named change |
| `/opsx:verify` | Validate before archive |
| `/opsx:sync` | Merge delta specs into main specs |
| `/opsx:archive` | Move completed change to `changes/archive/` |
