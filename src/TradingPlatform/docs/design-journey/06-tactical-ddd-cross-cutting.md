# Tactical DDD — cross-cutting

## Learning objective

For each bounded context, decide **layering** (Domain / Application / Infrastructure), what belongs in **TradingPlatform.Kernel**, and enforce the **dependency rule** (inward only). This doc is the checklist before you deep-dive each context (`07`–`12`).

## Prerequisites

- [05-ubiquitous-language-and-glossary-diff.md](./05-ubiquitous-language-and-glossary-diff.md)

## Workshop: dependency rule matrix

For each row, list **allowed project references** (what may depend on what).

| Project / layer | May reference | Must not reference |
|-----------------|---------------|--------------------|
| `*.Domain` | (ideally nothing external) | Infrastructure, Host, Cli |
| `*.Application` | Domain, Kernel (as designed) | Infrastructure specifics |
| `*.Infrastructure` | Application, Domain, Kernel | Host (register via extensions only) |
| `TradingPlatform.Kernel` | BCL only (goal) | Any `*.Infrastructure` |

## Workshop: what belongs in Kernel?

Kernel holds **published cross-context shapes** that are stable and broker-agnostic.

Fill “keep in Kernel vs move to a context”:

| Concept | Keep in Kernel? | If moved, destination |
|---------|------------------|------------------------|
| `SeriesDescriptor` | | |
| `OhlcBar` | | |
| `TimeFrameCode` | | |
| `TradingVectorId` / `TradingVectorSpec` | | |
| `OrderIntent` | | |
| `PositionSide` | | |

## Compare with repo

- Kernel types: [TradingPlatform.Kernel](../../src/BuildingBlocks/TradingPlatform.Kernel/) (`SeriesDescriptor`, `OhlcBar`, `TimeFrameCode`, `TradingVectorSpec`, `TradingVectorId`, `OrderIntent`, `PositionSide`, …).
- Per-context layering matches solution folders in [TradingPlatform.slnx](../../TradingPlatform.slnx).
- Composition: [TradingPlatform.Host/Program.cs](../../src/Hosts/TradingPlatform.Host/Program.cs), [TradingPlatform.Cli/Program.cs](../../src/Tools/TradingPlatform.Cli/Program.cs).

## Open questions / ADR candidates

- If `OrderIntent` grows exchange-specific fields, does Kernel stay pure by splitting DTO vs domain intent?
- Should Application projects ever reference **another** context’s Application, or only via interfaces defined by the upstream?

## Next doc

[07-context-market-data.md](./07-context-market-data.md)
