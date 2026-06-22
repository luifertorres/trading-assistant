# MarketData — Agent Instructions

## Purpose

Instrument registry, canonical OHLC store (SQLite), USD-M backfill, and live candle feeds.

## Key types

| Type | Project | Role |
|------|---------|------|
| `IInstrumentRegistry` | Application | Upsert/lookup instruments by exchange symbol |
| `ICandleSeriesReader` / `ICandleSeriesWriter` | Application | Read/write `OhlcBar` by `SeriesDescriptor` |
| `UsdmBackfillOrchestrator` | Application | Pages klines into SQLite with checkpoint |
| `ILiveCandleFeed` | Application | Multi-symbol closed-candle events |
| `BinanceUsdMBackfillExchange` | Infrastructure | Binance.Net REST klines + exchange info |
| `BinanceLiveCandleFeed` | Infrastructure | Binance.Net socket kline subscriptions |

## Do

- Use **Binance.Net** in Infrastructure only; map to `OhlcBar` at the edge.
- Support multiple `TimeFrameCode` values in candles table (e.g. `1D`, `4H`).
- Checkpoint backfill per instrument + timeframe.

## Don't

- Reference legacy `TradingAssistant` projects.
- Put broker types in Domain/Application public contracts.

## Verify

```bash
dotnet build src/platform/TradingPlatform/TradingPlatform.slnx
dotnet run --project src/platform/TradingPlatform/src/Tools/TradingPlatform.Cli -- backfill-4h --market-db .trading-platform-data/market.sqlite --data-root .trading-platform-data --symbols DOGEUSDT
```
