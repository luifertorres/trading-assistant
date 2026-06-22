# Synchronize editor devkit

Use when setting up a new clone, after changing files under `ai/commands/`, `ai/skills/`, or `ai/templates/`, or when editor workflow commands or rules are out of date.

## Constraints

- Canonical specs live only under `ai/` (commands, skills, templates, this manifest).
- Do not add editor product names or editor config folder paths into any file under `ai/`, `AGENTS.md`, `src/**/AGENTS.md`, `docs/`, or `.github/`.
- Generated files belong only under the detected editor config root at repo root (dot-prefixed folder).
- OpenSpec vendor files (`.cursor/commands/opsx-*`, `.cursor/skills/openspec-*`) are **not** managed by this bootstrap — see `openspec/SETUP.md`.

## Steps

1. **Detect `editorRoot`** — Use **one** target unless the user explicitly asks to refresh all editor roots:
   - **Default:** the active IDE's config folder at repo root (e.g. `.cursor/` when the session is Cursor).
   - **Explicit override:** use the folder the user names.
   - **Refresh all:** only when the user requests every dot-prefixed directory that already has `commands/`, `rules/`, or `skills/` — update each in turn; do not create extra roots unless asked.
   - If no folder exists and the IDE is unknown, ask which dot-prefixed folder to create (typically `.cursor/`).
2. **Read** [`ai/bootstrap/manifest.yaml`](../bootstrap/manifest.yaml).
3. **Rules** — Copy each file from `ai/templates/rules/` listed under `rules:` to `{editorRoot}/rules/` (overwrite).
4. **Commands** — For each command in `manifest.commands` except `synchronize-editor-devkit`:
   - Write `{editorRoot}/commands/<name>.md` as a slim workflow file per `slim_sections` and the canonical [`ai/commands/<name>.md`](.) content.
   - When a section is not inlined, add a pointer: "See `ai/commands/<name>.md`" (and the section name when helpful).
5. **Skill stubs** — For each entry under `skill_stubs:` in the manifest, write `{editorRoot}/skills/<name>/SKILL.md` with YAML frontmatter (`name`, `description` from canonical `ai/skills/<name>/SKILL.md` if present) and body: `Read and follow ai/skills/<name>/SKILL.md`.
6. **Report** — List `{editorRoot}/` files created/updated (overwrite each run is fine).

## Slim command recipes

| Command | Slim file must include |
|---------|-------------------------|
| `commit` | Constraints, Steps, Message style |
| `test-driven-implementation` | Constraints (incl. no secrets), Steps (read `ai/skills/test-driven-development` + `ai/skills/dotnet-verification`, integration csproj last) |
| `slice` | Title, Steps, pointer to ship-a-slice skill |

Do not emit a slim copy of `synchronize-editor-devkit` into `{editorRoot}/commands/`.
