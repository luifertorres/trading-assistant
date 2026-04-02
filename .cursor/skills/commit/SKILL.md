# Skill: Conventional Commit

## Description

Creates a git commit with a **short subject line**: first word is a **PascalCase English infinitive** verb (*Add*, *Update*, *Fix*, *Refactor*, …), no `type:` / `fix:` / `feat(scope):` labels. Optional body for context.

## When to Use

Use this skill when the user asks to commit changes, create a commit, or save progress.

## Instructions

### 1. Analyze Changes

Run `git status` and `git diff --staged` (or `git diff` if nothing is staged) to understand what changed.

### 2. Subject line (required)

**Hard limit: 50 characters maximum** for the subject (first line). If the draft is longer, shorten wording or drop non-essential detail; put extra context in the body.

**Format:**

1. **First word:** PascalCase, an **infinitive** verb (imperative style: the verb you would use after “to”—*Add*, *Update*, *Refactor*, *Fix*, *Store*, *Persist*, *Limit*, *Optimize*, *Implement*, *Remove*, *Rename*, …).
2. **After that:** rest of the summary in normal sentence casing (do not title-case every word unless it’s a proper noun).
3. **Do not** use labels or prefixes such as `fix:`, `feat:`, `chore:`, or `type(scope):`.

Examples of valid openings: `Fix Binance timeout in host`, `Add gap remediation handler`, `Update Binance.Net to 12.6.0`.

### 3. Body (optional)

Use a body only when the *why* is not obvious from the subject:

```
Subject line at most 50 chars

Optional body explaining WHY the change was made.
Wrap body at ~72 chars per line.
```

### 4. Execute

```bash
git add <relevant-files>
git commit -m "<subject>"
# or with body:
git commit -m "<subject>" -m "<body>"
```

### Rules

- **Never** commit `appsettings.Development.json` with real API keys.
- **Never** commit the SQLite database file (`trading.db`).
- Stage only related files — avoid mixing unrelated changes.
- **Subject line: ≤ 50 characters** (count spaces and punctuation).
- **First word:** PascalCase infinitive verb; **no** `fix:`, `feat:`, or other conventional-commit labels on the subject line.
- Use imperative sense throughout: *Fix* not *Fixed*, *Add* not *Added*.
- Write the commit message in English.

### Examples (subjects ≤ 50 chars)

```
Add Backtesting MVP CLI and synthetic series
```

```
Update Binance.Net to 12.6.0
```

```
Fix infinite Binance REST timeout in host
```

```
Refactor candle sync for smaller batches
```
