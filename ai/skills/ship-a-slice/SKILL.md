---
name: ship-a-slice
description: Start-of-session ritual — pick ONE deliverable slice with a visible proof, execute minimal steps, run the demo, append WINS.md. Use at session start or when the user runs /slice.
---

# Ship a slice

Forces **one visible win per session** aligned with the dopamine delivery plan.

## When to use

- Start of a work session on TradingPlatform or guardrails
- User runs `/slice` or asks "what should we ship today?"
- Task feels too large or unfocused

## Ritual (follow in order)

### 1. Pick ONE slice

Choose exactly one item from the active plan or [legacy-port-map.md](../../../src/platform/TradingPlatform/docs/legacy-port-map.md).

**Reject** slices that cannot be demo'd in the same session.

### 2. State proof up front

- **Visible artifact:** what the user will see (CLI output, file, Binance UI, etc.)
- **Proof command:** exact command to run (copy-paste ready)

### 3. Load minimal context

Read only:

- `.cursor/context/delivery-principles.md`
- Scoped `AGENTS.md` for contexts you touch
- `.cursor/rules/live-trading-safety.mdc` if touching live orders

### 4. Execute smallest steps

List 3–7 concrete steps. Implement only those. No drive-by refactors.

### 5. Run proof command

Execute the proof command in the terminal. Fix until it passes or report blocker.

### 6. Append WINS.md

Add one row to [WINS.md](../../../WINS.md):

```markdown
| YYYY-MM-DD | <slice-id> | <what was visible> | `<proof command>` |
```

### 7. Stop-and-show

Present the proof output to the user. Do **not** start the next plan todo unless they ask.

## Live trading extra checks

Before any `--arm` or real order:

- Latest backtest verdict for symbol is **PASS**
- Symbol is in the selected portfolio JSON
- `LiveTradingOptions.Armed` explicitly set
- Kill-switch file absent
- Build succeeded

Workflow command: [`ai/commands/slice.md`](../../commands/slice.md).
