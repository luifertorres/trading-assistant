# How to use this design trail

## Learning objective

Establish **your** working agreements for this folder: notation, timeboxing, where artifacts live, and how workshop output graduates into team-facing docs (ADRs, glossary).

## Prerequisites

- Read the platform overview: [README.md](../README.md).
- Skim existing decisions and language: [ADRs.md](../ADRs.md), [GLOSSARY.md](../GLOSSARY.md).

## Workshop: define your legend

Fill in the table (copy to a new section below when you start).

| Element | Your notation (example) |
|--------|-------------------------|
| Domain event | Orange sticky / past tense verb phrase |
| Command | Blue / imperative |
| Actor / role | Small yellow |
| Aggregate / consistency boundary | Purple cluster name |
| Policy / process | Pink “when X then Y” |
| Read model / query | Green |
| External system (exchange, DB file) | Red / hexagon |
| Hotspot (risk, ambiguity, conflict) | Black storm |

**Timeboxing:** suggest 45–90 minutes per numbered doc for first pass; revisit after coding.

**Physical vs digital:** if you use a wall, note where photos live (personal drive, not committed). In-repo, paste **mermaid** summaries.

## Repo tie-in: graduating decisions

When a workshop conclusion is stable:

1. Add or revise an ADR in [ADRs.md](../ADRs.md) (match existing tone: decision, rationale).
2. Update [GLOSSARY.md](../GLOSSARY.md) if ubiquitous language changed.
3. Optionally keep a scratch list in `pending-adrs.md` in this folder until implemented.

## Document template (repeat for 01–13)

Each workbook file should include:

1. **Learning objective**
2. **Prerequisites** (which prior docs in this trail)
3. **Workshop steps** (tables, diagrams—**you** fill)
4. **Compare with repo** (links to concrete types/files)
5. **Open questions / ADR candidates**

## Order of travel

`01` → `02` → `03` → `04` → `05` → `06` → per-context `07`–`12` → `13`.

Start tactical depth at **MarketData** (`07`) only after strategic docs (`01`–`05`) exist in draft form.

## Next doc

[01-problem-space-and-outcomes.md](./01-problem-space-and-outcomes.md)
