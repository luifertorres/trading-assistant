# Engineering principles (low churn)

Non-negotiables for agents working in this repository. Prefer updating the narrowly scoped file that owns a topic rather than duplicating long policy here.

## Architecture

- **Legacy monolith** (`src/TradingAssistant/`): dependency direction is strictly **Host → Infrastructure → Application → Domain**. Never reverse it. See root `AGENTS.md` and `.cursor/rules/architecture.mdc`.
- **Greenfield platform** (`src/TradingPlatform/`): modular monolith by bounded context; **no project references** to `TradingAssistant`, `CandlestickData`, or `Backtesting`. See `src/TradingPlatform/AGENTS.md` and `docs/ADRs.md`.

## Broker and domain boundaries

- **Binance.Net** is the supported .NET framework for exchange-facing code where applicable; **Domain and Application public contracts** stay broker-agnostic. Do not add new broker dependencies in Domain (legacy removal in progress). See root `AGENTS.md` and `.cursor/rules/binance-net.mdc`.

## Data and migrations

- **Never hand-edit EF migration files.** Use the `dotnet ef` commands documented in root `AGENTS.md` for the legacy host path. For TradingPlatform, follow whatever migration workflow is documented next to that host when it exists.

## Quality bar

- Match existing naming, file layout, and patterns in the touched area.
- Use `CancellationToken` on async APIs.
- Run or add tests appropriate to the change (see `.cursor/skills/dotnet-test/SKILL.md`).
