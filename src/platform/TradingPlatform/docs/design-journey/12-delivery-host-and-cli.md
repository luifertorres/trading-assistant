# Delivery — Host and CLI

## Learning objective

Treat **Delivery** as non-domain composition: wiring, process lifetime, paths on disk, and which contexts are referenced for **demo** vs **long-running host**.

## Strategic recap

- **Bounded context:** not a domain context—**composition root** and tooling.
- **Dependency:** Host/Cli depend on `*.Infrastructure` extension methods that register implementations.

## Prerequisites

- [11-context-execution.md](./11-context-execution.md)

## Scenario walk (fill)

| # | Scenario | Expected outcome |
|---|----------|------------------|
| 1 | Run CLI `demo` | End-to-end vertical slice under cwd `.trading-platform-data` |
| 2 | Run Host | Registers same services; uses `%LocalAppData%/TradingPlatform/` per [README.md](../README.md) |
| 3 | Cancel Host | `BackgroundService` loop stops cleanly |
| 4 | Add real market ingest worker | New hosted service module—decide which context it belongs to |

## Model sketch

- No aggregates; **configuration** and **hosting** concerns only.

## Workshop: composition checklist

List each `Add*Infrastructure` call and **which DB/path** it uses:

| Call | Data path (Host) | Data path (CLI) |
|------|------------------|-----------------|
| `AddMarketDataSqlite` | | |
| `AddResearchInfrastructure` | | |
| `AddPortfolioInfrastructure` | | |
| `AddAnalyticsInfrastructure` | | |
| `AddExecutionInfrastructure` | | |

## Compare with repo

| Artifact | Path |
|----------|------|
| Host composition | [TradingPlatform.Host/Program.cs](../../src/Hosts/TradingPlatform.Host/Program.cs) |
| CLI composition + demo | [TradingPlatform.Cli/Program.cs](../../src/Tools/TradingPlatform.Cli/Program.cs) |
| Build/run docs | [README.md](../README.md) |

## Open questions / ADR candidates

- Single Host vs split **read** and **write** deployables later?
- Configuration via `IOptions` vs hardcoded paths in Host.

## Next doc

[13-end-to-end-alignment-review.md](./13-end-to-end-alignment-review.md)
