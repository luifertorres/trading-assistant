# WebSocketTrading MVP

Isolated **Worker Service** that streams Binance USD-M Futures klines over WebSocket, evaluates Ivan Scherman's short SMA200/SMA5 vector in memory, and places **live** market orders via Binance.Net's **Websocket API**.

Documentation hub: [docs/README.md](../../../docs/README.md).

**Solution:** [`WebSocketTrading.slnx`](WebSocketTrading.slnx) (separate from Platform and Backtesting).

## Strategy

Source Pine vector: [`docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript`](../../../docs/pine-scripts/es1!-short-1d-sma200sma5.pinescript)

| Signal | Condition (on closed candle) |
|--------|------------------------------|
| Enter short | `SMA200[1] > SMA200` and `close > open` and `low > SMA5` |
| Exit short | Position is short and `close < SMA5` |

**Warmup:** keeps the last **201** closed candles in memory (`SMA200` needs `N` bars; first `N-1` values are null per [Skender SMA docs](https://dotnet.stockindicators.dev/indicators/sma#simple-moving-average-sma)).

## Defaults (smoke test)

| Setting | Default | Notes |
|---------|---------|-------|
| Symbol | `DOGEUSDT` | High liquidity, low min notional |
| Interval | `FiveMinutes` | Switch to `OneDay` in config for the real 1D vector |
| Notional | `$10` | Sized via exchange filters |
| Leverage | `1x` | Isolated margin |

## Credentials

Never commit API keys. Use **User Secrets** (project already has a `UserSecretsId`):

```bash
dotnet user-secrets set "Binance:ApiKey" "<your-key>" --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
dotnet user-secrets set "Binance:ApiSecret" "<your-secret>" --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

Or environment variables `Binance__ApiKey` and `Binance__ApiSecret`.

## Build / test

From repo root:

```bash
dotnet build src/mvp/WebSocketTrading/WebSocketTrading.slnx
dotnet test src/mvp/WebSocketTrading/WebSocketTrading.Tests/WebSocketTrading.Tests.csproj
```

## Run (live — real orders)

```bash
dotnet run --project src/mvp/WebSocketTrading/WebSocketTrading.Worker/WebSocketTrading.Worker.csproj
```

Override trading settings in `appsettings.json` or user secrets under the `Trading` section:

```json
{
  "Trading": {
    "Symbol": "DOGEUSDT",
    "Interval": "FiveMinutes",
    "NotionalUsd": 10,
    "Leverage": 1
  }
}
```

## Architecture

```
REST warmup (201 klines) → CandleBuffer
WS kline stream (Final only) → SmaShortStrategy → socket.UsdFuturesApi.Trading.PlaceOrderAsync
```

- **Market data:** public kline WebSocket streams ([Live Subscribing](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Live-Subscribing-Unsubscribing-to-streams))
- **Orders:** USD-M Websocket API ([WS API general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-api-general-info))
- **Package:** Binance.Net **13.1.1** (MVP only; Platform/legacy stay on 12.11.x)

## References

- [Binance.Net (JKorf)](https://github.com/JKorf/Binance.Net)
- [USD-M Futures REST general info](https://developers.binance.com/docs/derivatives/usds-margined-futures/general-info)
