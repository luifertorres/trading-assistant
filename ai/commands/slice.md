# Ship one visible win (/slice)

Run the **ship-a-slice** ritual. Full skill: `ai/skills/ship-a-slice/SKILL.md`.

**Input**: Optional slice hint (e.g. backtest DOGE, guardrails). If omitted, pick the next pending todo from the active plan or ask the user.

## Steps

1. Read `ai/context/delivery-principles.md`
2. Pick **one** slice with a concrete visible artifact and proof command
3. Load only scoped context (`AGENTS.md` for touched contexts; live-trading-safety rule if orders)
4. Implement minimal steps, run the proof command, append `WINS.md`
5. **Stop** after showing proof — do not continue unless the user asks

**Do not** start OpenSpec artifact creation unless the slice explicitly requires it.
