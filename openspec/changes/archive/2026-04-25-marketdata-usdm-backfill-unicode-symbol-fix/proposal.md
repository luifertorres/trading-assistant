## Why

The USD-M 1d backfill crashes on today's Binance universe. `SeriesTableNaming.ToPhysicalTableName` requires `^[A-Za-z0-9]+$`, but `fapi/v1/exchangeInfo` currently lists 3 TRADING PERPETUAL USDT contracts whose `symbol` contains CJK characters (`龙虾USDT`, `币安人生USDT`, `我踏马来了USDT`). Because those symbols sort last under `StringComparer.Ordinal`, the orchestrator runs through 534 symbols and then throws `ArgumentException: "Symbol must be alphanumeric."`, aborting the whole run with no per-symbol recovery. We need to unblock the backfill now without redesigning identity or storage — that larger work is scoped separately under `marketdata-instrument-identity-and-candles-registry`.

## What Changes

- Relax the domain rule in `SeriesTableNaming.ToPhysicalTableName` so any non-empty `Symbol` whose characters are **not** an ASCII control character and **not** a literal `"` is accepted. The physical name stays `"<Symbol>_<TimeFrame>"` and is always emitted inside SQLite's double-quoted identifier form (`"..."`), which is where the actual safety invariant lives.
- Keep the existing upper-casing of `Symbol` in the rendered table name and the existing time-frame safety check (`[A-Za-z0-9_-]`, `-` → `_`).
- Make `Usdm1dBackfillOrchestrator.RunAsync` resilient to per-symbol failures: any exception from series-name derivation, checkpoint update, exchange call, or writer for a single symbol is caught, logged at `Warning`, annotated on that symbol's checkpoint entry (new field, e.g. `LastErrorMessage` / `LastErrorAtUtc`, additive on `BackfillCheckpointSymbolEntryV1`), and the loop continues with the next symbol. The orchestrator returns normally; callers can inspect the checkpoint to see which symbols succeeded vs. failed.
- Record the identity debt (raw exchange string as domain identity + per-series table naming) in `src/platform/TradingPlatform/docs/ADRs.md` with a pointer to the follow-up change.
- No changes to the universe filter, to Binance.Net wiring, to rate limiting, to forward-paging semantics, or to `ICandleSeriesWriter`/`SqlitePerSeriesCandleStore`.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `trading-platform-marketdata-binance-1d-backfill`:
  - The "Daily series identity" scenario is broadened so the kernel `SeriesDescriptor` accepts any non-empty exchange symbol string (including Unicode and digit-prefixed names) and the persisted physical identifier remains safe under SQLite's quoted-identifier rules.
  - A new requirement (or scenario under the "Rate limiting and resumability" requirement) defines **per-symbol failure isolation**: a single symbol's failure MUST NOT abort the remainder of the run, and the failure MUST be visible on the checkpoint.

## Impact

- **Code:**
  - `src/platform/TradingPlatform/src/MarketData/MarketData.Domain/SeriesTableNaming.cs` — replace the alphanumeric predicate with a "no control chars, no `"`" predicate; keep the upper-case + timeframe assembly unchanged.
  - `src/platform/TradingPlatform/src/MarketData/MarketData.Application/Usdm1dBackfillOrchestrator.cs` — wrap the per-symbol body in `try/catch`, log + annotate the checkpoint entry, continue with the next symbol.
  - `src/platform/TradingPlatform/src/MarketData/MarketData.Application/BackfillCheckpointDocumentV1.cs` (or wherever the per-symbol entry lives) — additive optional fields for last error message / timestamp. No schema version bump; fields are optional on deserialization.
  - `src/platform/TradingPlatform/tests/MarketData.Domain.Tests/SeriesTableNamingTests.cs` — replace the "rejects `BTC-USDT`" assertion (it still rejects control chars and `"`, but accepts Unicode and digit-prefixed names); add positive tests for `龙虾USDT`, `1000PEPEUSDT`, `4USDT`.
  - `src/platform/TradingPlatform/tests/MarketData.Application.Tests/Usdm1dBackfillOrchestratorTests.cs` (new or extended) — fake exchange returning a mix of healthy + failing symbols; assert the loop finishes and checkpoint records the failure.
  - `src/platform/TradingPlatform/docs/ADRs.md` — short entry noting the identity debt and linking to the follow-up change.
- **Contracts / APIs:** Non-breaking. `SeriesDescriptor`, `ICandleSeriesWriter`, `IUsdM1dBackfillExchange`, and the CLI surface are unchanged.
- **Data:** No storage migration. Existing `.trading-platform-data/market.sqlite` and per-series tables continue to work. The fix does **not** add new rows for the CJK symbols until the orchestrator is rerun — which is exactly the intended behavior.
- **Dependencies:** None (no package changes).
- **Operations:** An operator rerunning the backfill after this change sees the three CJK perps ingested for the first time, plus any previously failing symbol retried on the next run. The run no longer aborts mid-universe on a single bad symbol.
- **Follows up:** `marketdata-instrument-identity-and-candles-registry` will retire `SeriesTableNaming`, introduce `InstrumentId` in the kernel, and move candles to a single table. This change deliberately does not anticipate those moves.