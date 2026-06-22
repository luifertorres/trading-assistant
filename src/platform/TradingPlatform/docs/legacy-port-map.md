# Legacy → Platform port map

Every **good** legacy capability mapped to a Platform bounded context, priority, and the reward it unlocks when shipped.

**Source:** `src/legacy/TradingAssistant/` — reference only; Platform must not reference legacy assemblies.

| Legacy feature | Location | Platform context | Priority | Unlocks (reward) |
|----------------|----------|------------------|----------|------------------|
| Realtime kline WebSocket + REST warm-up | `BinanceService.cs` | **MarketData** — `ILiveCandleFeed`, `BinanceLiveCandleFeed` | P0 | Live closed-candle logs; strategy eval without 4h wait |
| USD-M exchange info + kline REST backfill | `BinanceService.cs` | **MarketData** — `UsdmBackfillOrchestrator`, SQLite candles | P0 | Real 4H backtest data |
| Instrument registry + filters JSON | `BinanceService.TryGetSymbolInformation` | **MarketData** — `IInstrumentRegistry` | P0 | MARKET_LOT_SIZE sizing for live orders |
| Order placement (market entry) | `TradeHandler.cs` | **Execution** — `BinanceLiveOrderIntentSink` | P0 | Orders visible in Binance Desktop |
| Stop-loss StopMarket | `TradeHandler.cs` | **Execution** | P0 | Protected tiny positions |
| Leverage + ISOLATED margin config | `TradeHandler` / `BinanceService` | **Execution** | P0 | 1x isolated blast-radius cap |
| Position sizing (margin %, lot filters) | `TradeHandler.cs` | **Execution** | P0 | ~$5 min-notional entries |
| **Rsi5ExtremeStrategy** (active) | `Rsi5ExtremeStrategy.cs` | **Research** — `Rsi5ExtremeStrategy` | P0 | Shared backtest + live signals |
| SMA/RSI strategies (6 disabled) | `*Strategy.cs` in Host | **Research** | P2 | More vectors after cohort proves pipeline |
| User-data stream (fills, positions) | `BinanceService` user stream | **Execution** | P1 | Confirmed fills; position state |
| StopLossManager / TakeProfitManager | Host managers | **Execution** | P1 | SL/TP follow-ups after entry |
| TrailingStopManager / SteppedTrailing | Host managers | **Execution** | P2 | Optional risk refinement |
| BreakEvenWorker | Host | **Execution** | P2 | Move SL to entry |
| FASTER in-memory candle cache | `Faster/` | **MarketData** or Kernel | P2 | Hot-path perf; SQLite may suffice first |
| BTC correlation filter | `TradeHandler` | **Research** or **Portfolio** | P3 | Correlation gate on entries |
| Symbol exclusions list | `SymbolExclusions` | **Portfolio** or config | P3 | Skip bad symbols in live router |
| Telegram notifications | Host logging/Telegram | **Hosts** (notifications BC later) | P1 | Mobile pings on entries/exits |
| EF positions + migrations | `TradingAssistant.Infrastructure` | **Execution** persistence (future) | P2 | Restart-safe position book |
| MediatR signal pipeline | Application/Host | **Research** events + **Execution** router | P1 | Decouple feed from orders |
| CandlestickData API | `CandlestickData.*` | **MarketData** HTTP API (optional) | P3 | External chart consumers |
| Risk management config | `appsettings` RiskManagement | **Execution** `LiveTradingOptions` | P0 | Hard caps vs impulsive sizing |

## Cohort (first live vector set)

| Symbol | Vector | Status target |
|--------|--------|---------------|
| DOGEUSDT | long / 4H / Rsi5Extreme / 1x | Backtest PASS → portfolio → live |
| XRPUSDT | long / 4H / Rsi5Extreme / 1x | same |
| SOLUSDT | long / 4H / Rsi5Extreme / 1x | same |
| 1000PEPEUSDT | long / 4H / Rsi5Extreme / 1x | same |

## Sequencing (matches dopamine plan)

1. Guardrails + this map (docs win)
2. Strategy + 4H backfill + backtest verdict (PnL win)
3. Analytics rank + Portfolio compose (comparison win)
4. Live feed worker (candle win)
5. Tiny armed orders (Binance Desktop win)
6. User stream + managers + Telegram (management win)

See [ADRs.md](./ADRs.md) ADR-003 for simulation/live sink swap.
