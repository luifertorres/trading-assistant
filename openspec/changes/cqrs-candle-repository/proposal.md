## Why

Binance Futures now applies exponential back-off on critical endpoints (including klines), which can delay full historical candle ingestion for up to an hour and block startup readiness. We need a dedicated, asynchronous candlestick data service so the main trading app can start quickly while market data ingestion, recovery, and consistency checks run independently.

## What Changes

- Introduce an independent Candlestick Data API responsible for historical and realtime candle ingestion, storage, and retrieval.
- Add on-demand orchestration endpoints to start/resume, stop, and restart long-running historical downloads without coupling them to main app startup.
- Add query options to fetch locally stored candles by symbol set and time range, enabling the main app (and other clients) to consume data via REST.
- Delegate closed-candle websocket ingestion for multiple timeframes (1m, 5m, 15m, 1H, 1D, etc.) to this API so one service owns market-data continuity and normalization.
- Make the Candlestick Data API the canonical clock for trading: it publishes candle-closed events per timeframe when candles are persisted; the main app subscribes to events for timeframes used by active strategies, then polls for the full candlestick on event receipt (hybrid event-trigger + poll) to avoid race conditions from dual clocks and incomplete reads.
- Add gap-detection and gap-classification rules (normal exchange gap vs atypical missing data) and expose gap status so downstream signal processing can be suspended when data integrity is compromised.
- Evaluate and formalize storage and processing architecture for write/read concerns (single store vs CQRS/event-driven projection), with explicit decision criteria in design.

## Capabilities

### New Capabilities
- `candlestick-data-service`: Independent API lifecycle for historical/realtime candlestick ingestion and client-facing retrieval.
- `candlestick-gap-governance`: Detection, classification, and operational handling of missing-candle gaps, including integrity flags for downstream consumers.

### Modified Capabilities
- `market-data`: Shift historical/realtime candlestick responsibilities from the main app to the dedicated service and define startup interaction contract.
- `exchange-integration`: Delegate transport-level rate-limit/back-off handling to `Binance.Net` and `CryptoExchange.Net` while defining resilient sync orchestration (resume, checkpoints, idempotency).
- `trading-strategies`: Require signal evaluation to be inhibited whenever an atypical candle gap is active for the target symbol/timeframe.
- `architecture`: Define bounded-context boundaries, service contracts, and strategic DDD placement for Candlesticks as a separate context.

## Impact

- Affected systems: main Host startup flow, Infrastructure exchange implementations, websocket consumers, market-data pipelines, and strategy execution guards.
- New deployable/API surface: separate Candlestick Data API (REST control + query endpoints; background workers; health/progress state).
- Data layer impact: evaluation and likely introduction of time-series-optimized persistence and indexing strategy for candle ingestion and retrieval.
- Integration impact: main app calls this API via REST client during startup (async sync) and subscribes to candle-closed events for strategy timeframes; on event receipt, main app polls for full candlestick and passes to the strategy pipeline.
- Reliability/operations impact: requires resumable jobs, idempotent writes, progress tracking, and explicit integrity signaling for safe trading decisions.
- Dependency impact: define a release-monitoring practice for `Binance.Net` and `CryptoExchange.Net` updates to keep exchange behavior aligned without re-implementing client-level protections.
