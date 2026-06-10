# Legacy reference — shipped

**Target:** `src/TradingAssistant/CandlestickData.*` (independent Candlestick Data API).

**Status:** Implemented; main specs synced (`candlestick-data-service`, `candlestick-gap-governance`, and related deltas).

**Platform note:** TradingPlatform uses the canonical `candles` store and instrument registry instead of this per-series CQRS layout. Read this change for legacy live-bot market-data behavior only.
