# WebSocketTrading MVP

Isolated **Worker Service** that streams Binance USD-M Futures klines over WebSocket, evaluates Ivan Scherman's short SMA200/SMA5 vector in memory, and places **live** market orders via Binance.Net's **Websocket API**.

> **Live money:** There is no dry-run or testnet mode. Running the worker sends real orders to Binance USD-M Futures. Use a small notional and a liquid symbol with low minimum order size (e.g. `DOGEUSDT`, not `BTCUSDT`).

Documentation hub: [docs/README.md](../../../docs/README.md).

**Solution:** [`WebSocketTrading.slnx`](WebSocketTrading.slnx) — separate from Platform, legacy, and Backtesting.

## Projects

| Project | Role |
|---------|------|
| [`WebSocketTrading/`](WebSocketTrading/) | Pure strategy lib: `SmaShortStrategy`, `CandleBuffer`, `QuantitySizer` (no Binance types) |
| [`WebSocketTrading.Worker/`](WebSocketTrading.Worker/) | `BackgroundService` host; Binance.Net REST + WebSocket wiring |
| [`WebSocketTrading.Tests/`](WebSocketTrading.Tests/) | xUnit + FluentAssertions (strategy, buffer, sizing) |

## Strategy

Source Pine vector: [`docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript)

Evaluated on each **closed** candle only (`kline.Final == true`):

| Signal | Condition |
|--------|-----------|
| Enter short | Flat, `SMA200[1] > SMA200`, `close > open`, and `low > SMA5` |
| Exit short | Short and `close < SMA5` |
| Hold | Warmup incomplete, conditions unmet, or already in the desired state |

Indicators use [Skender.Stock.Indicators](https://dotnet.stockindicators.dev/) `GetSma(200)` and `GetSma(5)` on mapped `Quote` bars.

**Warmup:** keeps the last **201** closed candles in a fixed-size buffer. SMA200 needs `N` bars; the first `N-1` SMA values are null ([Skender SMA docs](https://dotnet.stockindicators.dev/indicators/sma#simple-moving-average-sma)). Comparing `SMA200[1]` to current SMA200 requires index ≥ 200, hence 201 bars.

## Configuration

Settings bind from the `Trading` section in `appsettings.json`, environment-specific overrides, user secrets, or environment variables (`Trading__Symbol`, etc.).

| Key | Default (`appsettings.json`) | Description |
|-----|------------------------------|-------------|
| `Symbol` | `DOGEUSDT` | USD-M perpetual symbol |
| `Interval` | `OneDay` | Binance `KlineInterval` name — **original 1D trading vector** |
| `NotionalUsd` | `5` | Target entry notional in USDT (sized through exchange filters) |
| `Leverage` | `1` | Initial leverage; worker sets **isolated** margin at startup |

**Environment overrides:**

| File | When | `Interval` |
|------|------|------------|
| [`appsettings.json`](WebSocketTrading.Worker/appsettings.json) | Production / base | `OneDay` |
| [`appsettings.Development.json`](WebSocketTrading.Worker/appsettings.Development.json) | `DOTNET_ENVIRONMENT=Development` (default for `dotnet run`) | `OneMinute` — faster smoke test of kline + order flow |

Code fallbacks in [`TradingOptions.cs`](WebSocketTrading.Worker/TradingOptions.cs) match the 1D vector (`OneDay`) when config is absent.

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

Unit tests cover strategy signals, ring-buffer eviction, and notional→quantity rounding. No live-exchange integration tests.

## Run

```bash
dotnet run --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

On startup the worker:

1. Loads `MARKET_LOT_SIZE` / `MIN_NOTIONAL` filters via REST `GetExchangeInfoAsync`
2. Sets isolated margin and leverage via REST
3. Syncs open short position via REST `GetPositionInformationAsync` (avoids double-entry after restart)
4. Warms up with the last 201 **closed** klines via REST `GetKlinesAsync`
5. Subscribes to the kline WebSocket stream; processes only **final** bars
6. On signal: places market orders via **`socket.UsdFuturesApi.Trading.PlaceOrderAsync`** (Websocket API)

**Order sizing:**

- **Enter short:** `NotionalUsd / price`, rounded up to `stepSize`, respecting `minQuantity` and `minNotional`
- **Exit short:** full open short quantity from REST position query, `reduceOnly: true`

## Architecture

```
REST  GetExchangeInfo / ChangeLeverage / GetPosition / GetKlines (warmup)
  └─► CandleBuffer (201 closed bars)

WS    SubscribeToKlineUpdatesAsync (Final only)
  └─► SmaShortStrategy.Evaluate
        └─► socket.UsdFuturesApi.Trading.PlaceOrderAsync (market)
```

| Concern | API | Binance.Net surface |
|---------|-----|---------------------|
| Kline stream | [WS market streams](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams) | `socket.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync` |
| Orders | [WS API trading](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info) | `socket.UsdFuturesApi.Trading.PlaceOrderAsync` |
| Warmup / account setup | [REST general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info) | `rest.UsdFuturesApi.ExchangeData` / `Account` |

**Package:** Binance.Net **13.1.1** in this MVP only. Platform and legacy remain on **12.11.x** — do not bump those when changing WebSocketTrading.

## Out of scope

- Platform / OpenSpec / legacy integration
- Dry-run, testnet, trailing stops, multi-symbol, persistence
- Production risk guardrails (kill-switch, daily caps) — see Platform Execution for that pattern

## Related MVP

Offline backtest CLI (separate solution): [`../Backtesting/README.md`](../Backtesting/README.md) — RSI strategy on mock/historical klines.

## References

- [Binance.Net (JKorf)](https://github.com/JKorf/Binance.Net)
- [USD-M Futures — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)
- [USD-M WS API — General Info](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info)
- [USD-M WS market streams](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams)
