# AI editor setup

Canonical **repo-native** agent specs live under [`ai/`](ai/) (skills, commands, rule templates). Root [`AGENTS.md`](AGENTS.md) is the routing index.

**OpenSpec** workflows are a separate external layer — committed vendor under `.cursor/commands/opsx-*` and `.cursor/skills/openspec-*`. See [`openspec/SETUP.md`](openspec/SETUP.md).

## Cursor (required after clone)

Generated slash commands, always-on TDD/plan rules, and repo-native skill stubs are **not** in git. Generate them locally:

1. Open an agent session in Cursor.
2. Run bootstrap: paste/run the workflow in [`ai/commands/synchronize-editor-devkit.md`](ai/commands/synchronize-editor-devkit.md).
3. Bootstrap writes to `.cursor/` (rules, commands, skill stubs pointing at `ai/skills/`).

Re-run bootstrap after any change under `ai/commands/`, `ai/skills/`, or `ai/templates/`.

### Tier 0 — always (required)

`synchronize-editor-devkit` → repo-native TDD, planning, verification, `/commit`, `/slice`, `/test-driven-implementation`.

### Tier 1 — Platform work without OpenSpec CLI

Read `openspec/changes/<name>/` artifacts directly (`tasks.md`, `proposal.md`, `design.md`). Use `implementation-planning` + `test-driven-development` skills. No `/opsx:*` required.

### Tier 2 — full OpenSpec (change authors)

Install CLI per [`openspec/SETUP.md`](openspec/SETUP.md).

## What is committed vs generated

| Path | In git | Notes |
|------|--------|--------|
| `ai/` | Yes | Repo-native canonical devkit |
| `EDITOR-AGENTS.md` | Yes | This file |
| `.cursor/context/`, `.cursor/plans/` | Yes | Routing and session plans |
| `.cursor/commands/opsx-*.md` | Yes | OpenSpec vendor |
| `.cursor/skills/openspec-*/` | Yes | OpenSpec vendor |
| `.cursor/rules/{architecture,binance-net,ddd,dotnet,live-trading-safety}.mdc` | Yes | Domain rules |
| `.cursor/rules/test-driven-development-enforcement.mdc` | No | Bootstrap from `ai/templates/rules/` |
| `.cursor/rules/implementation-plan-granularity.mdc` | No | Bootstrap from `ai/templates/rules/` |
| `.cursor/commands/{commit,slice,test-driven-implementation}.md` | No | Bootstrap slim commands |
| `.cursor/skills/{test-driven-development,dotnet-verification,...}/` | No | Bootstrap stubs → `ai/skills/` |

When repo-native workflow behavior changes, edit `ai/commands/` or `ai/skills/` and re-run bootstrap.

When OpenSpec vendor changes, run upstream `openspec update` and commit only `.cursor/opsx*` + `openspec-*` — never merge into `ai/`.
