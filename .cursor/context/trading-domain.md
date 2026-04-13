# Trading domain context (medium churn)

## Product scope

- **Exchange:** Binance **USD-M perpetual** (USDT-margined) is the primary operational context for the legacy trading assistant and related specs.
- **Greenfield** `TradingPlatform` models symbols, timeframes, simulation runs, portfolios, and execution intents in its own ubiquitous language.

## Authoritative vocabulary

- **TradingPlatform terms and bounded contexts:** [src/TradingPlatform/docs/GLOSSARY.md](../../src/TradingPlatform/docs/GLOSSARY.md) (e.g. `SeriesDescriptor`, `TimeFrameCode`, `TradingVectorSpec`, broker ACL).
- **Legacy layer concepts:** per-project `AGENTS.md` under `src/TradingAssistant/` (Domain entities, application orchestration, infrastructure adapters).

## Risk and operations

- Treat trading, position, and API-key paths as **high impact**: prefer explicit validation, logging, and clear failure modes over silent defaults.
- When behavior touches **real money or live orders**, require explicit user confirmation in the chat before assuming automation is desired.
