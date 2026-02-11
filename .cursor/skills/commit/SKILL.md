# Skill: Conventional Commit

## Description

Creates a git commit following the Conventional Commits specification, adapted for this trading-assistant project.

## When to Use

Use this skill when the user asks to commit changes, create a commit, or save progress.

## Instructions

### 1. Analyze Changes

Run `git status` and `git diff --staged` (or `git diff` if nothing is staged) to understand what changed.

### 2. Determine Commit Type

| Type | When to Use |
|------|-------------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `refactor` | Code restructuring without behavior change |
| `docs` | Documentation only |
| `test` | Adding or modifying tests |
| `chore` | Build, CI, dependencies, tooling |
| `style` | Formatting, whitespace (no logic change) |
| `perf` | Performance improvement |

### 3. Determine Scope

Use the layer or component name as scope:

| Scope | When |
|-------|------|
| `domain` | Changes in TradingAssistant.Domain |
| `application` | Changes in TradingAssistant.Application |
| `infrastructure` | Changes in TradingAssistant.Infrastructure |
| `host` | Changes in TradingAssistant (host project) |
| `strategy` | Strategy-related changes |
| `risk` | Risk management changes (SL, TP, TSL) |
| `binance` | Binance-specific adapter changes |
| `config` | Configuration changes |
| `deps` | Dependency updates |

### 4. Write Commit Message

Format:
```
type(scope): short description in imperative mood

Optional body explaining WHY the change was made.
```

Examples:
```
feat(domain): add TimeFrame enum to replace KlineInterval

Introduces a broker-agnostic TimeFrame enum as part of the
Binance.Net decoupling effort.
```

```
refactor(infrastructure): extract Binance adapter to separate class

Moves exchange-specific logic behind IExchangeService to support
future multi-broker scenarios.
```

### 5. Execute

```bash
git add <relevant-files>
git commit -m "<message>"
```

### Rules

- **Never** commit `appsettings.Development.json` with real API keys.
- **Never** commit the SQLite database file (`trading.db`).
- Stage only related files — avoid mixing unrelated changes.
- Keep the subject line under 72 characters.
- Use imperative mood: "add" not "added", "fix" not "fixed".
- Write the commit message in English.
