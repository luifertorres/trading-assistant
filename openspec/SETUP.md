# OpenSpec setup

OpenSpec is an **external framework** ([`@fission-ai/openspec`](https://github.com/Fission-AI/OpenSpec)). Agent slash commands and skills live in **committed vendor sources**:

- `openspec/agent/commands/opsx-*.md`
- `openspec/agent/skills/openspec-*/SKILL.md`

`/ai-onboard` copies them into local `.cursor/` (not in git). See [`EDITOR-AGENTS.md`](../EDITOR-AGENTS.md).

## Three tiers

| Tier | Requirement | What you get |
|------|-------------|--------------|
| **0** | Run [`ai/commands/ai-onboard.md`](../ai/commands/ai-onboard.md) | Full `.cursor/` including `/opsx:*`, TDD, `/commit`, `/slice` |
| **1** | `/ai-onboard` only (no CLI) | Read `openspec/changes/<name>/`; `/opsx:apply` markdown fallback |
| **2** | `/ai-onboard` + OpenSpec CLI | Full CLI workflows (`openspec new`, archive, sync) |

## Install CLI (Tier 2)

```bash
npm i -g @fission-ai/openspec
openspec --version
```

Optional future: repo-pinned `npx openspec` via root `package.json`.

### Bracket-only task lines

Checkbox lines in `tasks.md` **start with `[ ]` or `[x]`** (no leading list hyphen). See [`patches/README-fission-openspec-bracket-tasks.md`](patches/README-fission-openspec-bracket-tasks.md).

## Tier 1 — work without CLI

When `openspec --version` fails after `/ai-onboard`:

1. List active changes under `openspec/changes/` (exclude `archive/`).
2. Read `tasks.md`, `proposal.md`, `design.md`, `specs/` directly.
3. Use [`ai/skills/implementation-planning/SKILL.md`](../ai/skills/implementation-planning/SKILL.md) and [`ai/skills/test-driven-development/SKILL.md`](../ai/skills/test-driven-development/SKILL.md).
4. Install CLI when you need scaffold, status graphs, archive, or sync.

`/opsx:*` commands include CLI preflight — see `openspec/agent/commands/opsx-apply.md`.

## Refresh vendor sources

After upgrading `@fission-ai/openspec`:

1. Run upstream `openspec update` into a temp tree or manually merge into `openspec/agent/`.
2. Commit changes under `openspec/agent/` only.
3. Re-run `/ai-onboard` locally. **Do not** commit `.cursor/`.

## Workflow commands

| Slash | Use when |
|-------|----------|
| `/opsx:new` | Start a Platform change (CLI required to scaffold) |
| `/opsx:apply` | Implement tasks for a named change |
| `/opsx:verify` | Validate before archive |
| `/opsx:sync` | Merge delta specs into main specs |
| `/opsx:archive` | Move completed change to archive |
| `/opsx:continue` | Continue artifact creation |
| `/opsx:ff` | Fast-forward artifact creation |
| `/opsx:explore` | Design-only exploration |
| `/opsx:onboard` | Guided onboarding (CLI required) |
| `/opsx:bulk-archive` | Archive multiple changes |

Platform naming: changes prefixed `trading-platform-…`. See [`README.md`](README.md).
