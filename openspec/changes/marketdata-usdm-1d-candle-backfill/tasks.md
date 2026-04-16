# Tasks — marketdata-usdm-1d-candle-backfill

## 1. Dependencies and build surface

- [ ] 1.1 Add **Binance.Net** at the **latest stable** version to `[src/TradingPlatform/src/MarketData/MarketData.Infrastructure/MarketData.Infrastructure.csproj](../../../src/TradingPlatform/src/MarketData/MarketData.Infrastructure/MarketData.Infrastructure.csproj)` only; if the repo’s `AGENTS.md` or other projects pin a different Binance.Net version, reconcile (single version across solutions where practical).
- [ ] 1.2 Ensure `dotnet build src/TradingPlatform/TradingPlatform.slnx` succeeds after the reference.

## 2. Application layer (ports, orchestration)

- [ ] 2.1 Define broker-agnostic port(s) for the backfill (e.g. run options DTO: data root, market DB path, checkpoint path, optional snapshot) and a service interface to execute the backfill; **no** Binance types in public signatures. **Do not** add app-level sleeps or configurable inter-request **delay**—**Binance.Net** owns REST **rate limits and backoff** per [Binance USD-M general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info). **Infinite `HttpClient` request timeout** is configured when registering the client in **Infrastructure** (see §3.1).
- [ ] 2.2 Implement orchestration in **Application** (or a dedicated handler class) that: loads checkpoint, iterates symbols from an abstraction **implemented in Infrastructure**, fetches klines in **forward chronological** windows, calls `**ICandleSeriesWriter`**, updates checkpoint after each batch/symbol—per [design.md](design.md) and the delta spec.

## 3. Infrastructure — Binance USD-M adapter

- [ ] 3.1 Register / resolve `**IBinanceRestClient`** (or futures-specific client pattern from Binance.Net docs for the chosen package version) in DI for `**MarketData.Infrastructure`** (or CLI-only if design defers host wiring). Configure the underlying `**HttpClient` with infinite request timeout** (long backfills must not fail on default timeouts). Rely on **Binance.Net** for exchange **rate limits and retry/backoff** per [Binance USD-M general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info).
- [ ] 3.2 Implement **exchangeInfo** fetch and filter symbols to `**TRADING`** + `**PERPETUAL`** + `**USDT`** quote (triple from proposal/spec).
- [ ] 3.3 Implement **USD-M futures klines** fetch using interval string `**1d`** and `**startTime`/`endTime`** paging forward until no more rows; respect `limit` (e.g. 1500), compute the next window from the last bar’s `OpenTime`. The endpoint’s **request weight** is described in [Kline / Candlestick Data](https://developers.binance.com/docs/derivatives/usds-margined-futures/market-data/rest-api/Kline-Candlestick-Data); **Binance.Net** applies rate-limit and weight handling—no extra application-level throttling.
- [ ] 3.4 Add a **single mapper** from Binance kline rows to `**OhlcBar`** + validate `**SeriesDescriptor(symbol, TimeFrameCode.Day1)`**; all UTC / `DateTimeOffset` per spec.

## 4. Checkpoint file (v1)

- [ ] 4.1 Implement read/write of **one JSON file** (e.g. `backfill-1d-checkpoint.json` under configurable data root) with `**schemaVersion`**, `**runId`**, `**updatedAtUtc**`, `**marketDatabasePath**`, `**symbols[]**` (`symbol`, `complete`, `lastWrittenOpenTimeMs`).
- [ ] 4.2 On startup, **skip or resume** per symbol using checkpoint + `marketDatabasePath` match; document that **parallel runs** require separate data roots (see design).

## 5. Rate limiting and resilience

- [ ] 5.1 **No** operator-configurable **delay** between REST calls and **no** duplicate backoff policy in application code—**Binance.Net** implements **rate limits and retry/backoff** per [Binance USD-M general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info). Keep `**HttpClient` request timeout infinite** as in §3.1 so long requests are not cancelled by a default timeout.

## 6. Delivery — CLI

- [ ] 6.1 Add a **CLI subcommand** (e.g. `backfill-1d`) on `[TradingPlatform.Cli](../../../src/TradingPlatform/src/Tools/TradingPlatform.Cli/Program.cs)`: wire `**AddMarketDataSqlite`**, backfill service, Binance client with the **same registration as §3.1** (including **infinite HTTP timeout**), argparse for DB path, data root, checkpoint path, optional snapshot flag.
- [ ] 6.2 **Optional:** write `**exchangeInfo` snapshot JSON** when flag is set (path next to data root as in spec).

## 7. Verification

- [ ] 7.1 `**dotnet build`** the TradingPlatform solution.
- [ ] 7.2 **Unit tests (Domain):** cover domain/kernel types and rules used by the backfill path (e.g. `OhlcBar`, `SeriesDescriptor`, `TimeFrameCode.Day1` / naming invariants) so behavior is locked without manual checks.
- [ ] 7.3 **Integration tests (Infrastructure):** full-feature test that performs a **real USD-M kline fetch for `BTCUSDT`** `1d` (network-enabled test project or category), exercising the Binance adapter end-to-end through the same registration as production (**infinite HTTP timeout**, Binance.Net rate handling). Goal: **no reliance on manual CLI runs** for basic correctness; supports `/opsx-verify` and future regression checks.
- [ ] 7.4 If live-network tests are skipped in CI, document how to run them locally (env/filter) in README or test project README—**manual CLI** remains optional for operators, not the default verification path.

## 8. Documentation

- [ ] 8.1 Document the new command usage, checkpoint file location, and how to run **unit + integration** tests in `[src/TradingPlatform/README.md](../../../src/TradingPlatform/README.md)` (or CLI `--help` + test README—keep one discoverable location).
