# WebSocketTrading MVP

Isolated **Worker Service** that streams Binance USD-M Futures klines over WebSocket, evaluates Ivan Scherman **trading vectors** in memory, and places **live** market orders via Binance.Net's **Websocket API**.

> **Live money:** There is no dry-run or testnet mode. Running the worker sends real orders to Binance USD-M Futures. Use a small notional and a liquid symbol with low minimum order size (e.g. `DOGEUSDT`, not `BTCUSDT`).

Documentation hub: [docs/README.md](../../../docs/README.md).

**Solution:** [`WebSocketTrading.slnx`](WebSocketTrading.slnx) — separate from Platform, legacy, and Backtesting.

## Projects

| Project | Role |
|---------|------|
| [`WebSocketTrading/`](WebSocketTrading/) | Pure trading-logic lib: `Sma200Sma5TradingLogic`, `CandleBuffer`, `QuantitySizer` (no Binance types) |
| [`WebSocketTrading.Worker/`](WebSocketTrading.Worker/) | `BackgroundService` host; Binance.Net REST + WebSocket wiring |
| [`WebSocketTrading.Tests/`](WebSocketTrading.Tests/) | xUnit + FluentAssertions (trading logic, buffer, sizing) |

## Ubiquitous language

A **trading vector** is `(Asset, Direction, Timeframe, TradingLogic)`.

| Term | Meaning |
|------|---------|
| **Direction** | `Long` or `Short` |
| **Trading logic** | Entry/exit rules (e.g. `Sma200Sma5`) |
| **Position state** | `OutOfMarket` (no open qty for that vector), `Long`, or `Short` |

The worker runs **multiple vectors** concurrently when they share the same **Asset + Timeframe**. Default config runs **Short + Long** `Sma200Sma5` on `DOGEUSDT`.

## Trading logic (`Sma200Sma5`)

Source Pine vectors:

- Short: [`docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript)
- Long: [`docs/pine-scripts/es1!-long-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-long-1d-sma200sma5.pinescript)

Evaluated on each **closed** candle only (`kline.Final == true`). **Pyramiding:** enter fires whenever signal conditions hold, even if already in market; each enter adds another `NotionalUsd` fill. Exit closes the **full** side quantity.

| Direction | Enter (pyramiding) | Exit |
|-----------|-------------------|------|
| Short | `SMA200[1] > SMA200`, `close > open`, `low > SMA5` | Open short + `close < SMA5` |
| Long | `SMA200[1] < SMA200`, `close < open`, `high < MA5` | Open long + `close > MA5` |

Indicators use [Skender.Stock.Indicators](https://dotnet.stockindicators.dev/) `GetSma(200)` and `GetSma(5)` on mapped `Quote` bars.

**Warmup:** keeps the last **201** closed candles in a fixed-size buffer. SMA200 needs `N` bars; the first `N-1` SMA values are null ([Skender SMA docs](https://dotnet.stockindicators.dev/indicators/sma#simple-moving-average-sma)). Comparing `SMA200[1]` to current SMA200 requires index ≥ 200, hence 201 bars.

## Configuration

Settings bind from the `Trading` section in `appsettings.json`, environment-specific overrides, user secrets, or environment variables (`Trading__NotionalUsd`, etc.).

| Key | Default | Description |
|-----|---------|-------------|
| `NotionalUsd` | `5` | Target entry notional per fill in USDT |
| `Leverage` | `1` | Initial leverage; worker sets **isolated** margin at startup |
| `Vectors[]` | Short + Long on `DOGEUSDT` / `OneDay` | Each item: `Asset`, `Direction`, `Timeframe`, `TradingLogic` |

Example:

```json
"Trading": {
  "NotionalUsd": 5,
  "Leverage": 1,
  "Vectors": [
    { "Asset": "DOGEUSDT", "Direction": "Short", "Timeframe": "OneDay", "TradingLogic": "Sma200Sma5" },
    { "Asset": "DOGEUSDT", "Direction": "Long", "Timeframe": "OneDay", "TradingLogic": "Sma200Sma5" }
  ]
}
```

**Environment overrides:**

| File | When | Vectors |
|------|------|---------|
| [`appsettings.json`](WebSocketTrading.Worker/appsettings.json) | Production / base | Both on `OneDay` |
| [`appsettings.Development.json`](WebSocketTrading.Worker/appsettings.Development.json) | `DOTNET_ENVIRONMENT=Development` | Both on `OneMinute` — faster smoke test |

All vectors must share the same **Asset** and **Timeframe**. The worker enables **hedge (dual-side) position mode** at startup so long and short can both be open on the same symbol. Switching to hedge mode may fail if conflicting one-way positions are open — close them first in the Binance UI.

Binance credentials (required):

| Key | Source |
|-----|--------|
| `Binance:ApiKey` | User secrets or `Binance__ApiKey` env var |
| `Binance:ApiSecret` | User secrets or `Binance__ApiSecret` env var |

## Credentials

Never commit API keys. The Worker project has a `UserSecretsId` for local development:

```bash
dotnet user-secrets set "Binance:ApiKey" "<your-key>" --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
dotnet user-secrets set "Binance:ApiSecret" "<your-secret>" --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

## Build / test

From repo root:

```bash
dotnet build src/mvp/WebSocketTrading/WebSocketTrading.slnx
dotnet test src/mvp/WebSocketTrading/WebSocketTrading.Tests/WebSocketTrading.Tests.csproj
```

Unit tests cover trading-logic signals (short, long, pyramiding), ring-buffer eviction, and notional→quantity rounding. No live-exchange integration tests.

## Run

```bash
dotnet run --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

On startup the worker:

1. Loads `MARKET_LOT_SIZE` / `MIN_NOTIONAL` filters via REST `GetExchangeInfoAsync`
2. Enables **hedge mode** via REST `ModifyPositionModeAsync(true)` when needed
3. Sets **isolated** margin and leverage via REST
4. Syncs open long/short positions per vector via REST `GetPositionInformationAsync`
5. Warms up with the last 201 **closed** klines via REST `GetKlinesAsync`
6. Subscribes to one kline WebSocket stream; processes only **final** bars
7. On signal: places market orders via **`socket.UsdFuturesApi.Trading.PlaceOrderAsync`** with `positionSide`

**Order sizing:**

- **Enter:** `NotionalUsd / price`, rounded up to `stepSize`, respecting `minQuantity` and `minNotional` — adds to existing side (pyramiding)
- **Exit:** full open quantity for that `positionSide`, `reduceOnly: true`

## Architecture

```
REST  GetExchangeInfo / ModifyPositionMode / ChangeLeverage / GetPosition / GetKlines (warmup)
  └─► CandleBuffer (201 closed bars)

WS    SubscribeToKlineUpdatesAsync (Final only)
  └─► foreach trading vector: Sma200Sma5TradingLogic.Evaluate
        └─► socket.UsdFuturesApi.Trading.PlaceOrderAsync (market + positionSide)
```

| Concern | API | Binance.Net surface |
|---------|-----|---------------------|
| Kline stream | [WS market streams](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams) | `socket.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync` |
| Orders | [WS API trading](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info) | `socket.UsdFuturesApi.Trading.PlaceOrderAsync` |
| Warmup / account setup | [REST general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info) | `rest.UsdFuturesApi.ExchangeData` / `Account` |

**Package:** Binance.Net **13.1.1** in this MVP only. Platform and legacy remain on **12.11.x** — do not bump those when changing WebSocketTrading.

## Out of scope

- Platform / OpenSpec / legacy integration
- Dry-run, testnet, trailing stops, multi-asset vectors, persistence
- Production risk guardrails (kill-switch, daily caps) — see Platform Execution for that pattern

## Related MVP

Offline backtest CLI (separate solution): [`../Backtesting/README.md`](../Backtesting/README.md) — RSI strategy on mock/historical klines.

## References

- [Binance.Net (JKorf)](https://github.com/JKorf/Binance.Net)
- [USD-M Futures — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)
- [USD-M WS API — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info)
- [USD-M WS market streams](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams)
