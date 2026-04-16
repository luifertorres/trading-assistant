# MarketData.Infrastructure integration tests

## Live Binance network tests

Tests that call the public Binance USD-M REST API are **skipped by default** so CI and offline runs stay deterministic.

To run them locally:

1. Set environment variable `RUN_TRADINGPLATFORM_LIVE_BINANCE` to any non-empty value (for example `1`).
2. Run:

```bash
dotnet test src/TradingPlatform/tests/MarketData.Infrastructure.IntegrationTests/MarketData.Infrastructure.IntegrationTests.csproj
```

Or filter by class name:

```bash
dotnet test src/TradingPlatform/TradingPlatform.slnx --filter "FullyQualifiedName~BinanceUsdM1dBackfillExchangeLiveTests"
```

No API keys are required for market-data endpoints used by these tests.
