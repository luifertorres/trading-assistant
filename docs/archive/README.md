# Archived planning documents

These files are kept for historical context. They are **not** active guidance—use the superseding docs listed below.

| Archived doc | Summary | Superseded by |
|--------------|---------|---------------|
| [architecture-preferences.md](architecture-preferences.md) | Early Clean Architecture + DDD preferences; broker-agnostic domain intent | [engineering-principles](../../ai/context/engineering-principles.md), Platform [ADRs](../../src/platform/TradingPlatform/docs/ADRs.md) |
| [backtesting-strategic-ddd.md](backtesting-strategic-ddd.md) | Strategic DDD plan for backtesting module (Spanish) | Platform Research + [design-journey/08](../../src/platform/TradingPlatform/docs/design-journey/08-context-research.md); OpenSpec `trading-platform-research-backtest-cli` |
| [event-driven-architecture.md](event-driven-architecture.md) | Event-driven RSI(5) DCA architecture plan (Spanish) | Future Execution intents in Platform |

Legacy multi-project refactor and CandlestickData work were reverted; the live bot is again a single-project monolith under `src/legacy/TradingAssistant/`.
