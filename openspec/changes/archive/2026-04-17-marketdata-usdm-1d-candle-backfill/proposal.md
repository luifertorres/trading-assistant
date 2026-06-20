## Why

TradingPlatform’s MarketData context can persist per-series OHLCV bars, but nothing today ingests **real** Binance USD-M futures history—only synthetic demo data. Research and future workflows need a **trustworthy daily history** for all actively traded USDT perpetuals without manual one-off downloads.

## What Changes

- Add a **batch backfill** path that discovers **active USD-M USDT perpetual** symbols (Binance `exchangeInfo`-style filtering: e.g. `TRADING`, `PERPETUAL`, `USDT` quote) and, for each symbol, fetches **full `1d` kline history** via Binance Futures REST (paged), mapping into `OhlcBar` + `SeriesDescriptor`.
- Persist bars through existing `**ICandleSeriesWriter`** into the current **SQLite per-series** store (`MarketData.Infrastructure`); reads remain ordered by `OpenTime` as today.
- Provide a **delivery** entry point (e.g. CLI command or host one-shot) to run the backfill with **exchange rate limits and backoff handled by Binance.Net**, **resumable checkpoints** per symbol (and optional global progress), and clear logging—so restarts do not redo completed symbols.
- Introduce **Binance.Net** (or equivalent) only at the **Infrastructure** boundary; Domain/Application public types stay broker-agnostic per platform rules.
- Optionally persist a **snapshot** of the symbol universe used for the run (JSON or small table) so the backfill set is **reproducible** for audits and backtests.

## Capabilities

### New Capabilities

- `trading-platform-marketdata-binance-1d-backfill`: Bulk ingest of Binance USD-M Futures **1d** klines for all **currently active trading** USDT perpetual symbols into TradingPlatform MarketData storage, with paging, **Binance.Net–mediated** exchange limits/backoff, and resume semantics.

### Modified Capabilities

- None. Existing `openspec/specs/market-data` and `openspec/specs/exchange-integration` describe the **legacy** TradingAssistant / Candlestick Data paths; this change adds **greenfield** TradingPlatform behavior without altering those requirement sets.

## Impact

- **Code:** `src/platform/TradingPlatform/src/MarketData/` (new Application ports and/or use cases, Infrastructure Binance adapter), `TradingPlatform.Kernel` (only if new shared types are justified), `TradingPlatform.Cli` and/or `TradingPlatform.Host` (orchestration, DI wiring).
- **Dependencies:** **Binance.Net** at the **latest stable** version on MarketData Infrastructure (reconciled with any repo-wide package pin); broker-facing code stays behind Infrastructure per platform rules.
- **Data:** Growth of `market.sqlite` (or configured path) by **one physical table per symbol** for `1d` series; size bounded by years listed × number of symbols (daily granularity).
- **Operations:** Long-running job; **API weight/rate limits** are enforced **by Binance.Net** (no separate app delay knob); suitable for manual or scheduled runs, not assumed to run inside tight startup budgets.