## 1. Service Skeleton and Contracts

- [x] 1.1 Create `Candlestick Data` bounded-context solution structure (Domain/Application/Infrastructure/Host) with Clean Architecture dependency rules and interfaces in Domain/Application, implementations in Infrastructure.
- [x] 1.2 Define API contracts for sync commands (`start-or-resume`, `stop`, `restart`) and queries (`candles` with symbols/timeframe/from/to, `integrity-status` with observability fields, `sync-status`).
- [x] 1.3 Implement sync command endpoints (`POST /sync/start-or-resume`, `POST /sync/stop`, `POST /sync/restart`) that invoke resumable workers.
- [x] 1.4 Implement main-app REST client integration to trigger asynchronous `start-or-resume` during startup.

## 2. Persistence and Data Model

- [x] 2.1 Create candle persistence schema keyed by (`symbol`, `timeframe`, `open_time`) with uniqueness constraints and range-query indexes; retention indefinite.
- [x] 2.2 Implement sync checkpoint persistence per (`symbol`, `timeframe`) for deterministic resume/restart.
- [x] 2.3 Implement integrity status persistence (`Eligible`, `Compromised`, `Recovering`) per (`symbol`, `timeframe`) with reason and gap metadata.
- [x] 2.4 Add local development profile with SQLite fallback and environment configuration aligned with production target.

## 3. Historical and Realtime Ingestion

- [x] 3.1 Implement historical sync worker that resumes from first chronologically missing candle and writes idempotently for multiple timeframes.
- [x] 3.2 Implement lifecycle controls so stop/restart are durable and safe across process restarts.
- [x] 3.3 Migrate closed-candle websocket ingestion for multiple timeframes (1m, 5m, 15m, 1H, 1D, etc.) to the new service and route through the shared validation/persistence pipeline.
- [x] 3.4 Implement sync status reporting with progress, lag, and last checkpoint metadata.
- [x] 3.5a Publish candle-closed event (symbol, timeframe, openTime) when candles are persisted.
- [x] 3.5b Choose and integrate event delivery mechanism (message queue, gRPC stream, or WebSocket) for candle-closed events.

## 4. Query Endpoints and Candlestick Retrieval

- [x] 4.1 Implement `GET /candles` with symbols, timeframe, from, to; return completeness metadata (`isComplete`, `fromOpenTime`, `toOpenTime`, `missingRanges`).
- [x] 4.2 Implement `GET /integrity-status` with observability fields (`status`, `reason`, `gapRange`, `lastVerifiedOpenTime`, `detectedAt`).
- [x] 4.3 Implement `GET /sync/status` with job progress and per-symbol/timeframe state.

## 5. Gap Governance and Trading Safety

- [x] 5.1 Implement gap detection and classification rules for normal exchange gaps vs atypical missing-candle gaps.
- [x] 5.2 Implement remediation workflow to backfill atypical gaps and transition integrity status from `Recovering` to `Eligible` after continuity verification.
- [x] 5.3 Integrate integrity checks in the main app indicator/signal path: fetch integrity status from Candlestick Data API before running indicators and signals; inhibit processing when integrity is `Compromised`; record inhibited signals with reason code `CANDLE_INTEGRITY_COMPROMISED`.
- [x] 5.4 Add audit logging for gap classification decisions and signal inhibition reasons.

## 6. Main App Event Subscription and Pipeline Integration

- [x] 6.1 Implement main app subscription to candle-closed events for timeframes used by active strategies.
- [x] 6.2 On candle-closed event receipt, poll Candlestick Data API for full candlestick (symbol, timeframe, time range) and pass to indicator and strategy pipeline.
- [x] 6.3 Align trading decision trigger with candle-closed events as the canonical clock.

## 7. Operational Hardening and Dependency Governance

- [x] 7.1 Add health/readiness endpoints and basic runbook metrics for sync throughput, freshness lag, and gap counts.
- [x] 7.2 Add compatibility smoke tests for `Binance.Net`/`CryptoExchange.Net` upgrades and pin/update policy automation.
- [x] 7.3 Add feature-flagged rollout path and rollback switch to revert ingestion ownership if needed.
- [x] 7.4 Validate end-to-end acceptance scenarios from specs (startup delegation, resumable sync, integrity gating, event-triggered poll, query correctness).
