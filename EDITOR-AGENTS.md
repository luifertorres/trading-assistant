# AI editor setup

Canonical agent specs are **not** in `.cursor/`. The entire `.cursor/` directory is **local-only** (gitignored). Run `/ai-onboard` after every clone.

## Committed sources

| Layer | Path | Role |
|-------|------|------|
| Repo-native devkit | [`ai/`](ai/) | Skills, commands, rule templates, routing context, [`onboard manifest`](ai/onboard/manifest.yaml) |
| OpenSpec vendor | [`openspec/agent/`](openspec/agent/) | `opsx-*` commands and `openspec-*` skills (external framework) |
| Specs | [`openspec/`](openspec/) | Platform capabilities and changes |
| Index | [`AGENTS.md`](AGENTS.md) | Task routing |

## Required after clone

1. Open an agent session in Cursor.
2. Run [`ai/commands/ai-onboard.md`](ai/commands/ai-onboard.md) (or `/ai-onboard` if already onboarded once on this machine).

Onboarding writes the full local editor tree:

```
.cursor/
├── context/     ← from ai/context/
├── rules/       ← from ai/templates/rules/
├── commands/    ← openspec/agent/commands/ + slim repo-native commands
└── skills/      ← openspec/agent/skills/ + repo-native stubs → ai/skills/
```

Re-run `/ai-onboard` after changes under `ai/`, `ai/context/`, `ai/templates/`, or `openspec/agent/`.

### OpenSpec CLI (optional Tier 2)

Not installed by `/ai-onboard`. Install when you need `/opsx:new`, archive, or sync — see [`openspec/SETUP.md`](openspec/SETUP.md). `/opsx:*` commands recommend install on first use if CLI is missing.

### Work without OpenSpec CLI (Tier 1)

Read `openspec/changes/<name>/` markdown directly; use `ai/skills/implementation-planning` and `ai/skills/test-driven-development`.

## Maintainer notes

- **Repo-native changes:** edit `ai/` → re-run `/ai-onboard`.
- **OpenSpec vendor refresh:** update `openspec/agent/` (from upstream `openspec update`) → re-run `/ai-onboard` → commit `openspec/agent/` only — never commit `.cursor/`.
