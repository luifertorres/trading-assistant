---
name: /slice
id: slice
category: Workflow
description: "Ship one visible win this session — pick a slice, prove it, log WINS.md"
---

Run the **ship-a-slice** ritual (`.cursor/skills/ship-a-slice/SKILL.md`).

**Input**: Optional slice hint after `/slice` (e.g. `/slice backtest DOGE` or `/slice guardrails`). If omitted, pick the next pending todo from the dopamine delivery plan or ask the user which reward they want today.

**You MUST:**

1. Read [delivery-principles.md](../context/delivery-principles.md)
2. Pick **one** slice with a concrete visible artifact and proof command
3. Load only scoped context (`AGENTS.md` for touched contexts; live-trading-safety rule if orders)
4. Implement the minimal steps, run the proof command, append [WINS.md](../../WINS.md)
5. **Stop** after showing proof — do not continue to the next plan phase unless the user asks

**Do not** start OpenSpec artifact creation unless the slice explicitly requires it.
