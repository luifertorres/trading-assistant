---
name: commit
description: Create a git commit with a short PascalCase-infinitive subject line (≤50 chars). Use when the user asks to commit changes.
---

# Skill: Conventional Commit

## Description

Creates a git commit with a **short subject line**: first word is a **PascalCase English infinitive** verb (*Add*, *Update*, *Fix*, *Refactor*, …), no `type:` / `fix:` / `feat(scope):` labels. Optional body for context.

## When to Use

Use this skill when the user asks to commit changes, create a commit, or save progress.

## Instructions

### 1. Analyze Changes

Run `git status` and `git diff --staged` (or `git diff` if nothing is staged) to understand what changed.

### 2. Subject line (required)

**Hard limit: 50 characters maximum** for the subject (first line).

**Format:**

1. **First word:** PascalCase, an **infinitive** verb.
2. **After that:** rest of the summary in normal sentence casing.
3. **Do not** use labels or prefixes such as `fix:`, `feat:`, or `type(scope):`.

### 3. Body (optional)

Use a body only when the *why* is not obvious from the subject.

### 4. Execute

```bash
git add <relevant-files>
git commit -m "<subject>"
```

### Rules

- **Never** commit `appsettings.Development.json` with real API keys.
- **Never** commit the SQLite database file (`trading.db`).
- Stage only related files — avoid mixing unrelated changes.
- **Subject line: ≤ 50 characters**.
- Write the commit message in English.

Workflow command: [`ai/commands/commit.md`](../../commands/commit.md).
