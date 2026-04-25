# OpenSpec: bracket-only task lines (`@fission-ai/openspec`)

Task lines in `tasks.md` use **no leading list hyphen** so Markdown previews render checkboxes clearly:

```markdown
[ ] 1.1 Do the thing
[x] 1.2 Done
```

**Legacy** lines still work:

```markdown
- [ ] 1.1 …
- [x] 1.1 …
```

## Patching a global `npm i -g @fission-ai/openspec`

The CLI in `%APPDATA%\npm\node_modules\@fission-ai\openspec\` is patched in-tree on this machine to:

1. **`dist/commands/workflow/instructions.js`**  
   - `parseTasksFile`: regex accepts optional `[-*]\s*` before `[` / `[x]`.  
   - Apply output lists tasks as `[ ] description` (no leading `-`).

2. **`dist/utils/task-progress.js`**, **`dist/commands/change.js`**  
   - `TASK_PATTERN` / `COMPLETED_TASK_PATTERN` use `^(?:[-*]\s*)?\[` so bracket-first lines count.

3. **`dist/commands/schema.js`**  
   - Default `tasks` artifact template uses bracket-only lines.

4. **`schemas/spec-driven/templates/tasks.md`**  
   - Spec-driven `tasks` scaffold uses bracket-only lines.

After **`npm i -g @fission-ai/openspec`**, re-apply the same edits (or upstream them into `@fission-ai/openspec`).
