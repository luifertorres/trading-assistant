# WebSocketTrading MVP

Isolated **Worker Service** that streams Binance USD-M Futures klines over WebSocket, evaluates Ivan Scherman **trading vectors** in memory, and places **live** market orders via Binance.Net's **Websocket API**.

> **Live money:** There is no dry-run or testnet mode. Running the worker sends real orders to Binance USD-M Futures. Use a small notional and a liquid symbol with low minimum order size (e.g. `DOGEUSDT`, not `BTCUSDT`).

Documentation hub: [docs/README.md](../../../docs/README.md).

**Solution:** [`WebSocketTrading.slnx`](WebSocketTrading.slnx) — separate from Platform, legacy, and Backtesting.

## Projects

| Project | Role |
|---------|------|
| [`WebSocketTrading/`](WebSocketTrading/) | Pure trading-logic lib: `Sma200Sma5TradingLogic`, `CandleBuffer`, `QuantitySizer`, `TradingVectorCatalog`, `VectorInventory` (no Binance types) |
| [`WebSocketTrading.Worker/`](WebSocketTrading.Worker/) | `BackgroundService` host; Binance.Net REST + WebSocket wiring |
| [`WebSocketTrading.Tests/`](WebSocketTrading.Tests/) | xUnit + FluentAssertions (trading logic, buffer, sizing) |

## Ubiquitous language

A **trading vector** is `(Asset, Direction, Timeframe, TradingLogic)`.

| Term | Meaning |
|------|---------|
| **Direction** | `Long` or `Short` |
| **Trading logic** | Entry/exit rules (e.g. `Sma200Sma5`) |
| **Position state** | `OutOfMarket` (no open qty for that vector), `Long`, or `Short` |

Each vector identity must be **unique** — duplicate `(Asset, Direction, Timeframe, TradingLogic)` tuples are rejected. Vectors may use **different Timeframes** on the same Asset; the worker subscribes to one kline stream per distinct timeframe. Default config runs **Short + Long** `Sma200Sma5` on `DOGEUSDT` / `OneDay`.

**Per-vector inventory:** Binance hedge mode exposes one Long and one Short position per symbol. When multiple vectors share Asset + Direction (e.g. Long on `OneDay` and Long on `OneMinute`), each vector tracks its own fill quantity via `VectorInventory`. Exits close only that vector's tracked quantity (`min(tracked, exchangeSide)`), not the full exchange side.

**Startup seed:** On restart, if multiple vectors share a Direction and the exchange side has open quantity, the **first configured** vector of that Direction receives the full side quantity; others start at zero. A warning is logged. Per-vector fill history is not persisted across restarts.

## Trading logic (`Sma200Sma5`)

Source Pine vectors:

- Short: [`docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript)
- Long: [`docs/pine-scripts/es1!-long-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-long-1d-sma200sma5.pinescript)

Evaluated on each **closed** candle only (`kline.Final == true`). **Pyramiding:** enter fires whenever signal conditions hold, even if already in market; each enter adds another `NotionalUsd` fill to that vector's inventory. Exit closes only that vector's tracked quantity.

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

All vectors must share the same **Asset**; **Timeframe** may differ per vector. The worker enables **hedge (dual-side) position mode** at startup so long and short can both be open on the same symbol. Switching to hedge mode may fail if conflicting one-way positions are open — close them first in the Binance UI.

Mixed-timeframe example:

```json
"Vectors": [
  { "Asset": "DOGEUSDT", "Direction": "Short", "Timeframe": "OneDay", "TradingLogic": "Sma200Sma5" },
  { "Asset": "DOGEUSDT", "Direction": "Long", "Timeframe": "OneMinute", "TradingLogic": "Sma200Sma5" }
]
```

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

Unit tests cover trading-logic signals (short, long, pyramiding), ring-buffer eviction, notional→quantity rounding, vector catalog validation, and per-vector inventory. No live-exchange integration tests.

## Run

```bash
dotnet run --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

On startup the worker:

1. Loads `MARKET_LOT_SIZE` / `MIN_NOTIONAL` filters via REST `GetExchangeInfoAsync`
2. Enables **hedge mode** via REST `ModifyPositionModeAsync(true)` when needed
3. Sets **isolated** margin and leverage via REST
4. Seeds per-vector inventory from exchange positions (first vector per Direction gets full side qty)
5. Warms up with the last 201 **closed** klines per distinct timeframe via REST `GetKlinesAsync`
6. Subscribes to one kline WebSocket stream per distinct timeframe; processes only **final** bars
7. On signal: places market orders via **`socket.UsdFuturesApi.Trading.PlaceOrderAsync`** with `positionSide`

**Order sizing:**

- **Enter:** `NotionalUsd / price`, rounded up to `stepSize`, respecting `minQuantity` and `minNotional` — adds to vector inventory (pyramiding)
- **Exit:** vector's tracked quantity (`min(tracked, exchangeSide)`), `reduceOnly: true`

## Architecture

```
REST  GetExchangeInfo / ModifyPositionMode / ChangeLeverage / GetPosition / GetKlines (warmup per TF)
  └─► CandleBuffer per Timeframe (201 closed bars each)

WS    SubscribeToKlineUpdatesAsync per Timeframe (Final only)
  └─► foreach matching-TF vector: Sma200Sma5TradingLogic.Evaluate
        └─► VectorInventory (AddFill on enter / ConsumeForExit on exit)
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
- **Equal risk per vector** (e.g. max 2% of account per vector on a $5000 account) — pending Ivan Scherman research, including whether pyramiding remains in scope

## Related MVP

Offline backtest CLI (separate solution): [`../Backtesting/README.md`](../Backtesting/README.md) — RSI strategy on mock/historical klines.

## References

- [Binance.Net (JKorf)](https://github.com/JKorf/Binance.Net)
- [USD-M Futures — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)
- [USD-M WS API — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info)
- [USD-M WS market streams](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams)
