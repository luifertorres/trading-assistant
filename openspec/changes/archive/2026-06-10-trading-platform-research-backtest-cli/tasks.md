# Tasks — trading-platform-research-backtest-cli

## 1. CLI argument parsing

- [x] 1.1 Add `BacktestArgs` record and `Parse(string[] args)` in `TradingPlatform.Cli` with flags from [design.md](design.md): `--market-db`, `--research-db`, `--symbol`, `--from`, `--to`, `--strategy`, `--enter-bar`, `--exit-bar`, `--initial-capital`, `--fee-bps`, `--position-fraction`, `--save`.
- [x] 1.2 Validate required `--market-db`; default `--symbol` to `BTCUSDT`, `--strategy` to `FixedWindow`, research DB to `<cwd>/.trading-platform-data/research.sqlite` when `--save` or when research DI is registered.
- [x] 1.3 Parse `--from` / `--to` as UTC `DateTimeOffset` (ISO-8601); reject unknown flags with usage text.

## 2. Backtest command handler

- [x] 2.1 Route `backtest` in `Program.cs` main switch (before `demo` default); update usage line to include `backtest`.
- [x] 2.2 Implement `RunBacktestAsync`: bootstrap `AddMarketDataSqlite(marketDb)` + `AddResearchInfrastructure(researchDb)` + logging (no Analytics/Portfolio/Execution).
- [x] 2.3 Resolve instrument via `IInstrumentRegistry.GetByExchangeSymbolAsync("binance", "usdm", "perpetual", symbol)`; exit non-zero with backfill hint when null.
- [x] 2.4 Build `TradingVectorSpec` with `TimeFrameCode.Day1`, `PositionSide.Long`, strategy kind and `enterBar`/`exitBar` parameters; build `SimulationConfiguration` from CLI flags.
- [x] 2.5 Call `IBacktestRunner.RunAsync(new BacktestRequest(vector, cfg, from, to))`; if bar count is zero (pre-read via `ICandleSeriesReader` or zero-trade empty result), exit non-zero with clear message.
- [x] 2.6 Log summary: `InstrumentId`, bar count, `RunId`, trade count, `FinalEquity`, `MaxDrawdownFraction`; when `--save`, call `ISimulationRunRepository.SaveAsync` and log research DB path.

## 3. Tests

- [x] 3.1 Add CLI or application-level test: seed registry + `Day1` bars in temp SQLite, run backtest logic (extract handler to testable static if needed), assert non-zero trades for `FixedWindow` with valid enter/exit indices.
- [x] 3.2 Add test for missing instrument (non-zero exit / exception) and empty bar series.
- [x] 3.3 Run `dotnet build src/TradingPlatform/TradingPlatform.slnx` and `dotnet test` on affected test projects.

## 4. Documentation

- [x] 4.1 Update `src/TradingPlatform/README.md` with `backtest` command example, flag table, and two-step workflow (`backfill-1d` then `backtest` for `BTCUSDT`).
- [x] 4.2 Update `TradingPlatform.Cli` usage string in `Program.cs` to list `backtest` flags at a high level.
