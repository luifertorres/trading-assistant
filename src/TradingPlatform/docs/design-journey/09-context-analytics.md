# Context deep dive — Analytics

## Learning objective

Treat analytics as **pure functions** over persisted run results: ranking, correlation matrices, and other metrics—**no** broker I/O, **no** strategy execution.

## Strategic recap

- **Subdomain:** supporting (enables portfolio construction) unless novel metrics are your product.
- **Upstream/downstream:** downstream of Research (`SimulationRunResult`); upstream to Portfolio composition policies that consume metrics.

## Prerequisites

- [08-context-research.md](./08-context-research.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | Rank runs by return | Ordered `RunMetrics` |
| 2 | Build underwater correlation matrix | Symmetric pairwise coefficients ([ADRs.md](../ADRs.md) **ADR-005**) |
| 3 | Single run input | Define degenerate matrix behavior |
| 4 | Mismatched equity timestamps across runs | Define alignment rule (current engine assumes comparable series—verify) |

## Model sketch

- **VO / read models:** `RunMetrics` — [RunMetrics.cs](../../src/Analytics/Analytics.Domain/RunMetrics.cs).
- **Aggregate:** often **none**; analytics is a **domain service** package. If you introduce `AnalyticsStudy` as aggregate, justify lifecycle.

## Application ports

| Port | Responsibility |
|------|------------------|
| `IRunAnalytics` | `RankByReturn`, `UnderwaterCorrelationMatrix` — [IRunAnalytics.cs](../../src/Analytics/Analytics.Application/IRunAnalytics.cs) |

## Infrastructure choices

- In-process engine registration: [Analytics.Infrastructure/ServiceCollectionExtensions.cs](../../src/Analytics/Analytics.Infrastructure/ServiceCollectionExtensions.cs).
- Engine: [RunAnalyticsEngine.cs](../../src/Analytics/Analytics.Infrastructure/RunAnalyticsEngine.cs).

## Compare with repo

| Artifact | Path |
|----------|------|
| Interface | [IRunAnalytics.cs](../../src/Analytics/Analytics.Application/IRunAnalytics.cs) |
| Metrics VO + calculator | [RunMetrics.cs](../../src/Analytics/Analytics.Domain/RunMetrics.cs) |
| Implementation | [RunAnalyticsEngine.cs](../../src/Analytics/Analytics.Infrastructure/RunAnalyticsEngine.cs) |

## Open questions / ADR candidates

- **ADR-005:** alternatives (Spearman, copula, regime-conditioned correlation).
- Should Analytics depend on **Research.Domain** types forever, or introduce a **narrow DTO** owned by Analytics?

## Next doc

[10-context-portfolio.md](./10-context-portfolio.md)
