# OpenSpec setup

OpenSpec is an **external framework** ([`@fission-ai/openspec`](https://github.com/Fission-AI/OpenSpec)). Agent slash commands and skills for OpenSpec live in **committed vendor paths**:

- `.cursor/commands/opsx-*.md`
- `.cursor/skills/openspec-*/SKILL.md`

They are **not** part of the repo-native `ai/` devkit. See [`EDITOR-AGENTS.md`](../EDITOR-AGENTS.md).

## Three tiers

| Tier | Requirement | What you get |
|------|-------------|--------------|
| **0** | Run [`ai/commands/synchronize-editor-devkit.md`](../ai/commands/synchronize-editor-devkit.md) | TDD, planning, verification, `/commit`, `/slice` |
| **1** | None (markdown only) | Read `openspec/changes/<name>/` and implement with TDD skills |
| **2** | OpenSpec CLI | Full `/opsx:*` workflows |

## Install CLI (Tier 2)

```bash
npm i -g @fission-ai/openspec
openspec --version
```

Optional future: repo-pinned `npx openspec` via root `package.json` (not configured today).

### Bracket-only task lines

This repo uses checkbox lines in `tasks.md` that **start with `[ ]` or `[x]`** (no leading list hyphen). See [`patches/README-fission-openspec-bracket-tasks.md`](patches/README-fission-openspec-bracket-tasks.md) if you patch a global CLI install.

## Tier 1 — work without CLI

When `openspec --version` fails:

1. List active changes: directories under `openspec/changes/` **excluding** `archive/`.
2. Read `openspec/changes/<name>/tasks.md`, `proposal.md`, `design.md`, and `specs/` directly.
3. Follow [`ai/skills/implementation-planning/SKILL.md`](../ai/skills/implementation-planning/SKILL.md) and [`ai/skills/test-driven-development/SKILL.md`](../ai/skills/test-driven-development/SKILL.md).
4. Install CLI when you need `openspec new`, status graphs, archive, or sync.

Committed `/opsx:*` commands include CLI-less fallback where practical — see `.cursor/commands/opsx-apply.md`.

## Refresh vendor files

After upgrading `@fission-ai/openspec`:

1. Run upstream `openspec update` (or reinstall per OpenSpec docs).
2. Commit only changed files under `.cursor/commands/opsx-*` and `.cursor/skills/openspec-*`.
3. **Do not** copy OpenSpec skills into `ai/`.

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
