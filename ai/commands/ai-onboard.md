# AI onboard

Use when setting up a new clone, after changing files under `ai/`, `openspec/agent/`, or when editor workflow commands or rules are out of date.

**Required after every clone** — the entire `.cursor/` directory is local-only (not in git).

## Constraints

- Canonical specs live under `ai/` (commands, skills, templates, context) and `openspec/agent/` (OpenSpec vendor).
- Do not add editor product names or editor config folder paths into any file under `ai/`, `AGENTS.md`, `src/**/AGENTS.md`, `docs/`, or `.github/`.
- Generated files belong only under the detected editor config root at repo root (dot-prefixed folder, typically `.cursor/`).

## Steps

1. **Detect `editorRoot`** — Default: `.cursor/` at repo root when using Cursor. Use one target unless the user asks to refresh all editor roots.
2. **Read** [`ai/bootstrap/manifest.yaml`](../bootstrap/manifest.yaml).
3. **Context** — Copy every file from `ai/context/` to `{editorRoot}/context/` (overwrite).
4. **Rules** — Copy every `*.mdc` from `ai/templates/rules/` to `{editorRoot}/rules/` (overwrite).
5. **OpenSpec vendor commands** — Copy every `*.md` from `openspec/agent/commands/` to `{editorRoot}/commands/` (overwrite). Preserve CLI preflight blocks in those files.
6. **OpenSpec vendor skills** — For each `openspec/agent/skills/<name>/SKILL.md`, copy to `{editorRoot}/skills/<name>/SKILL.md` (overwrite).
7. **Repo-native commands** — For each entry under `manifest.commands`, write slim `{editorRoot}/commands/<name>.md` per `slim_sections` and canonical [`ai/commands/<name>.md`](.).
8. **Repo-native skill stubs** — For each entry under `skill_stubs:`, write `{editorRoot}/skills/<name>/SKILL.md` with YAML frontmatter from `ai/skills/<name>/SKILL.md` and body: `Read and follow ai/skills/<name>/SKILL.md`.
9. **OpenSpec CLI (report only)** — Run `openspec --version 2>&1` or echo missing. Do **not** install automatically. If missing, report: install via [`openspec/SETUP.md`](../../openspec/SETUP.md); `/opsx:*` commands will recommend install on first use.
10. **Report** — List `{editorRoot}/` areas updated: context, rules, openspec commands/skills, repo-native commands, skill stubs; note OpenSpec CLI status.

Do not emit a slim copy of `ai-onboard` into `{editorRoot}/commands/`.

## Slim command recipes

| Command | Slim file must include |
|---------|-------------------------|
| `commit` | Constraints, Steps, Message style |
| `test-driven-implementation` | Constraints (incl. no secrets), Steps |
| `slice` | Title, Steps, pointer to ship-a-slice skill |

## Maintainer: refresh OpenSpec vendor

When upgrading `@fission-ai/openspec`, update committed sources under `openspec/agent/` (not `{editorRoot}/`), then re-run `/ai-onboard`. See [`openspec/SETUP.md`](../../openspec/SETUP.md).
