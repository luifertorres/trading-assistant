---
name: pr
description: Create a GitHub pull request with standardized format for the trading-assistant project.
---

# Skill: Create Pull Request

## Description

Creates a GitHub pull request with a standardized format for the trading-assistant project.

## When to Use

Use this skill when the user asks to create a PR, open a pull request, or submit changes for review.

## Instructions

### 1. Gather Context

```bash
git log main..HEAD --oneline
git diff main..HEAD --stat
git status
```

### 2. Determine Base Branch

- Default base: `develop`
- If the user specifies a different base, use that.
- For hotfixes: base on `main`.

### 3. PR Title Format

```
type(scope): short description
```

Examples:
- `feat(domain): add broker-agnostic TimeFrame enum`
- `refactor(infrastructure): decouple BinanceService from domain`
- `fix(host): resolve RSI worker crash on empty candle set`

### 4. PR Body Template

```markdown
## Summary

- Brief description of WHAT changed and WHY (2-3 bullet points max)

## Changes

- List of specific changes organized by layer/component

## Architecture Impact

- [ ] Domain layer remains infrastructure-agnostic
- [ ] No new infrastructure dependencies in Application
- [ ] DI registration updated in Program.cs (if applicable)
- [ ] Migrations generated (if schema changed)

## Test Plan

- [ ] Build succeeds (`dotnet build`)
- [ ] Unit tests pass (`dotnet test` on affected csproj)
- [ ] Integration tests when Infrastructure changed

## Related

- Link to OpenSpec change or planning documents
```

### 5. Execute

```bash
git push -u origin HEAD
gh pr create --base develop --title "type(scope): description" --body "..."
```

### Rules

- Always push the branch before creating the PR.
- Reference OpenSpec specs when the PR implements a Platform change.
- Never force-push to `main` or `develop`.
