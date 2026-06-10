## Context

Binance Futures candle ingestion can be slow during constrained exchange periods, and startup currently couples trading readiness with historical preload. The new change introduces a dedicated Candlestick Data API so the trading app can initialize quickly while candle synchronization continues asynchronously.

Current architecture is Clean Architecture + DDD, with strict layer boundaries and broker-agnostic domain rules. The design must preserve those constraints while introducing cross-service integration and deterministic gap governance.

Key constraints:
- Keep transport-level throttling, retries, and protocol behavior delegated to `Binance.Net` and `CryptoExchange.Net`.
- Ensure historical sync is resumable, idempotent, and restartable from persisted checkpoints.
- Block signal processing for symbol/timeframe streams with unresolved atypical candle gaps.
- Keep API contracts explicit between the trading app and the new service.
- Interfaces live in Domain (business-specific) or Application (cross-cutting); implementations live in Infrastructure.

## Goals / Non-Goals

**Goals:**
- Decouple candle ingestion lifecycle from trading app startup.
- Centralize historical + realtime candle ownership in one dedicated service.
- Support operational control (`start/resume`, `stop`, `restart`) and query-by-symbol, timeframe, and time range.
- Provide robust integrity governance (normal gaps vs atypical gaps) and expose eligibility flags.
- Choose a storage architecture that supports append-heavy writes, ordered range reads, and data integrity checks.
- Decide whether CQRS and dual databases are necessary for this phase.

**Non-Goals:**
- Re-implement exchange client transport internals already provided by `Binance.Net` and `CryptoExchange.Net`.
- Introduce full event-sourcing across all domains.
- Migrate every market-data concern in one release; only candlestick ownership is in scope.
- Build a generic multi-exchange market data platform in this change.

## Decisions

### Decision 1: Candlesticks become a separate bounded context and deployable service
Candlesticks are treated as a distinct bounded context (`Candlestick Data`) with its own Domain/Application/Infrastructure/Host projects.

**Why:** It isolates high-volume ingestion and continuity concerns from trade orchestration concerns, and allows independent scaling, deployment, and recovery.

**Alternatives considered:**
- Keep candlesticks inside the main app: rejected due to startup coupling and operational contention.
- Split only into background workers in the same host: rejected because API control/query contracts and independent lifecycle are first-class requirements.

### Decision 2: Start with logical CQRS (separate command/query models) on a single physical database
Use command/query separation in code, but keep one physical datastore for both write and read paths in phase 1.

**Why:** This gives clean boundaries and future extensibility without introducing immediate dual-write/eventual-consistency complexity.

**Alternatives considered:**
- Full CQRS with write DB + read DB and event projections: deferred; adds failure modes (projection lag, replay complexity, dual operational burden) not required for initial goals.
- No CQRS at all (single mixed model): rejected because lifecycle commands and query contracts become harder to evolve independently.

### Decision 3: Use PostgreSQL + TimescaleDB for candle storage
Store candles in a hypertable keyed by (`symbol`, `timeframe`, `open_time`) with uniqueness and range-query indexes.

**Why:** The workload is append-heavy time series with range retrieval and continuity checks. TimescaleDB provides mature SQL semantics, partitioning, compression, and retention features while fitting .NET operational patterns.

**Retention:** Candlesticks are retained indefinitely (no cold-storage eviction). Every strategy may use candle data partially or fully.

**Alternatives considered:**
- SQLite only: acceptable for small local development, but weak for concurrent service workloads and long-term operational scaling.
- Keeping FASTER as primary store: good for low-latency cache, but weaker for durable analytical/range query requirements as a system-of-record.
- Dedicated event store first: rejected for phase 1 complexity.

### Decision 4: Single ingestion pipeline with checkpointed synchronization state
Historical sync and realtime closed-candle ingestion for multiple timeframes (1m, 5m, 15m, 1H, 1D, etc.) feed the same validation and persistence pipeline. Sync progress is tracked per (`symbol`, `timeframe`) checkpoint.

**Why:** One canonical pipeline avoids drift between historical and realtime paths and simplifies idempotency guarantees.

**Alternatives considered:**
- Separate historical and realtime persistence stacks: rejected due to divergence risk and duplicate logic.

### Decision 5: Gap governance as first-class policy
Maintain a `CandleIntegrityStatus` per (`symbol`, `timeframe`) that can be `Eligible`, `Compromised`, `Recovering`. Trading app consumes this status and blocks strategy execution when compromised. Inhibited signals are recorded with reason code `CANDLE_INTEGRITY_COMPROMISED`.

**Why:** This translates data-quality uncertainty into explicit operational safety behavior.

**Alternatives considered:**
- Best-effort logging only: rejected because hidden data gaps can trigger unsafe signals.

### Decision 5b: Integrity-status observability
The `integrity-status` response SHALL include: `status` (Eligible|Compromised|Recovering), `reason` (NORMAL_GAP|ATYPICAL_GAP|REMEDIATING|UNKNOWN), `gapRange` { fromOpenTime, toOpenTime } when applicable, `lastVerifiedOpenTime`, and `detectedAt` for dashboards and runbooks.

### Decision 6: Integration contract between services
Expose REST endpoints:
- Command: `POST /sync/start-or-resume`, `POST /sync/stop`, `POST /sync/restart`
- Query: `GET /candles?symbols=&timeframe=&from=&to=` (returns `isComplete`, `fromOpenTime`, `toOpenTime`, `missingRanges`), `GET /integrity-status`, `GET /sync/status`

Main app startup triggers `start-or-resume` asynchronously, then proceeds with readiness based on available eligible data.

**Why:** Explicit contracts preserve bounded-context separation and avoid direct DB coupling.

### Decision 7: Candlestick Data API as canonical clock (hybrid event-trigger + poll)
The Candlestick Data API SHALL publish candle-closed events (symbol, timeframe, openTime) when candles are persisted. The main app subscribes to events for timeframes used by active strategies; on event receipt, the main app polls for the full candlestick and passes it to the strategy pipeline. Event delivery mechanism (message queue, gRPC stream, or WebSocket) is chosen in implementation.

**Why:** Avoids race conditions from dual clocks (main app vs service) and ensures candles are persisted before consumption. Polling alone would risk incomplete reads; events provide the clock signal.

**Alternatives considered:**
- Polling only: rejected due to timing drift and possible incomplete candlestick reads before persistence.
- Push-only (event carries full candlestick): deferred; increases payload and coupling; poll-on-event keeps contracts simpler.

### Decision 8: Dependency release monitoring policy
Track `Binance.Net` and `CryptoExchange.Net` releases as an operational practice; upgrade deliberately with compatibility tests.

**Why:** Exchange behavior changes are best absorbed through maintained client libraries rather than custom transport logic.

## Risks / Trade-offs

- [Operational complexity increases with a new service] -> Mitigation: start with minimal endpoints, health checks, and clear ownership runbooks.
- [TimescaleDB introduces new infrastructure dependency] -> Mitigation: local-dev profile with SQLite fallback and migration scripts for staging/prod.
- [Eventual consistency between ingestion and trading decisions] -> Mitigation: enforce integrity/status checks before indicator/signal processing.
- [Incorrect gap classification can block valid trading or allow unsafe trading] -> Mitigation: codify deterministic classification rules, audit logs, and manual override workflow.
- [Library updates may change behavior unexpectedly] -> Mitigation: pin versions, run scheduled compatibility smoke tests, and maintain upgrade checklist.

## Migration Plan

1. Create `Candlestick Data` bounded-context projects and service host.
2. Implement schema and repositories for candles, sync checkpoints, and integrity status.
3. Implement command endpoints (`start/resume`, `stop`, `restart`) with resumable workers.
4. Implement query endpoints (`candles`, `integrity-status`, `sync-status`) with spec-defined metadata.
5. Move websocket closed-candle ingestion for multiple timeframes (1m, 5m, 15m, 1H, 1D, etc.) to the new service and unify with historical pipeline.
6. Implement candle-closed event publication (mechanism TBD: message queue, gRPC stream, or WebSocket).
7. Integrate main app REST client startup call; make startup non-blocking.
8. Main app subscribes to candle-closed events for strategy timeframes; on event, poll for full candlestick and pass to pipeline.
9. Add strategy guard in main app to skip signal processing on compromised integrity (reason code `CANDLE_INTEGRITY_COMPROMISED`).
10. Roll out behind a feature flag; run parallel validation mode before fully switching traffic.

Rollback strategy:
- Disable feature flag to revert to current ingestion path.
- Keep existing strategy pipeline unchanged until integrity service is proven stable.

## SLO Targets

| SLO | Target | Notes |
|-----|--------|------|
| Realtime freshness | 1m candle persisted and event published within 5s of exchange close | Keeps strategies reacting near real-time |
| Historical sync progress | No strict SLO; report per-symbol progress % | Startup sync can take long under exchange limits |
| Restart recovery | Sync job resumes from checkpoint within 30s of restart | Validates resumability |
| Gap remediation | Best-effort under 1h for actively traded symbols | Prioritize symbols affecting live strategies |

Refine targets after measuring production behavior.

## Resolved (previously Open Questions)

- **Event flow:** Hybrid event-trigger + poll (see Decision 7). Candlestick Data API publishes candle-closed events; main app subscribes and polls for full candlestick on receipt.
- **Retention:** Candlesticks retained indefinitely (Decision 3).
- **Integrity observability:** Decision 5b defines `status`, `reason`, `gapRange`, `lastVerifiedOpenTime`, `detectedAt`.
- **Sync health SLO:** See SLO Targets above.

## Open for Implementation

- Choice of event delivery mechanism (message queue vs gRPC stream vs WebSocket) for candle-closed events.
