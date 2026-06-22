# Commit staged changes

Use when the user wants to create a git commit from **already staged** files.

## Constraints

- **Staged only**: Do not stage new files unless the user explicitly asks. Commit what is in the index.
- **Message length**: The commit message (subject line) must be **at most 50 characters**, including spaces. If the user's text is longer, shorten it while keeping the intent, or ask for a shorter line before committing.
- **No secrets**: Do not put tokens, passwords, or other secrets in the message.
- **Never** commit `appsettings.Development.json` with real API keys or `trading.db`.

## Steps

1. Confirm there is something staged: run `git diff --cached --stat` (or equivalent). If nothing is staged, stop and tell the user to stage files first.
2. Obtain the commit message from the user's request or the chat. Normalize to a single-line subject (no multi-paragraph bodies unless the user insists; if you add a body, the **first line** must still be ≤ 50 characters).
3. Verify character count for the subject line (≤ 50). Adjust if needed.
4. Run: `git commit -m "<message>"` with the validated message. Use the repository root as the working directory.
5. Report success with the short hash from the command output, or show the error if the commit failed.

## Message style

- First word: PascalCase English infinitive verb (*Add*, *Update*, *Fix*, *Refactor*, …).
- **No bracket prefix** and **no** `fix:`, `feat:`, or conventional-commit labels on the subject line.
- Imperative mood, present tense.
- If the user asks for their **usual** style, match **recent commits on the branch** (`git log --oneline -15`).

See also `ai/skills/commit/SKILL.md` for extended examples.
