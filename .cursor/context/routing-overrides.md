# Routing overrides (high churn)

Log **routing corrections** so the repo’s instructions can compound. This file is **evidence**, not policy.

## How to add an entry

When the assistant picked the wrong workflow, wrong tree (Platform vs legacy), wrong layer, or wrong OpenSpec command:

1. Append a row under **Log** with date, short request summary, what was tried, and what you wanted instead.
2. If **three** similar mistakes occur for the same pattern, add **one** consolidated bullet to `routing-map.md` (or a scoped `.cursor/rules/*.mdc` change) and link it here under **Promoted rules**.

## Promotion rule (manual)

- **3 similar overrides** → promote a single durable rule (one bullet in `routing-map.md` or a small rule file update). Keep the promoted text minimal and testable.

## Log

| Date | Request (summary) | Recommended / executed | Override (what was correct) |
|------|--------------------|---------------------------|-----------------------------|
| _example_ | _“Add indicator X”_ | _Started in legacy Domain_ | _Should use TradingPlatform Research_ |

## Promoted rules

- _(Links or short descriptions of rules that were added elsewhere from evidence.)_
