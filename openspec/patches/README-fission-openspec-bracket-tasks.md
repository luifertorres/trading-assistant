# OpenSpec: bracket-only task lines (`@fission-ai/openspec`)

In **this repo’s OpenSpec flow**, `tasks.md` **must** use checkbox lines that **start with `[ ]` or `[x]`** — **no** leading list hyphen (`-`). That is what Markdown previews and editors treat as task checkboxes in this workflow.

**Do this (required for new and edited `tasks.md`):**

```markdown
[ ] 1.1 Do the thing
[x] 1.2 Done
```

**Do not** add a bullet before the checkbox in new work:

```markdown
- [ ] 1.1 …   ← not used in this flow for new task lines
```

The **patched** global CLI still **parses** older `- [ ]` / `- [x]` lines so existing changes and archives keep working, but **agents and humans writing tasks here** should use bracket-only lines only.

## Patching a global `npm i -g @fission-ai/openspec`

The CLI in `%APPDATA%\npm\node_modules\@fission-ai\openspec\` is patched on this machine so that:

1. **`dist/commands/workflow/instructions.js`**
   - `parseTasksFile`: regex accepts lines that start with `[` **or** optional `[-*]\s*` before `[`.
   - Apply output lists tasks as `[ ] description` (no leading `-`).

2. **`dist/utils/task-progress.js`**, **`dist/commands/change.js`**
   - `TASK_PATTERN` / `COMPLETED_TASK_PATTERN` use `^(?:[-*]\s*)?\[` so bracket-first lines count.

3. **`dist/commands/schema.js`**
   - Default `tasks` artifact template uses bracket-only lines.

4. **`schemas/spec-driven/templates/tasks.md`**
   - Spec-driven `tasks` scaffold uses bracket-only lines.

After **`npm i -g @fission-ai/openspec`**, re-apply the same edits (or upstream them into `@fission-ai/openspec`).
