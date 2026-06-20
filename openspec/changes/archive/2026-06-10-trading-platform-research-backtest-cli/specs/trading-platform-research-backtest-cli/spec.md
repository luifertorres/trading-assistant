## ADDED Requirements

### Requirement: Backtest CLI subcommand

`TradingPlatform.Cli` SHALL expose a `backtest` subcommand that runs a single Research simulation against persisted MarketData candles without seeding synthetic bars.

#### Scenario: Operator invokes backtest

- **WHEN** the operator runs `TradingPlatform.Cli backtest` with required `--market-db <path>` and valid strategy parameters
- **THEN** the CLI MUST resolve the target instrument from the registry, read `Day1` bars from that database, invoke `IBacktestRunner.RunAsync`, and print a run summary to stdout

#### Scenario: Unknown subcommand unchanged

- **WHEN** the operator runs `TradingPlatform.Cli` with `demo` or `backfill-1d`
- **THEN** existing behavior MUST remain unchanged

### Requirement: Market database and instrument resolution

The backtest command SHALL require `--market-db` pointing at a SQLite database populated by MarketData (instrument registry and canonical candles store). The command MUST resolve `InstrumentId` via `IInstrumentRegistry` using venue `binance`, market `usdm`, contract type `perpetual`, and the operator-supplied exchange symbol (default `BTCUSDT`).

#### Scenario: Symbol found in registry

- **WHEN** the registry contains an instrument matching the requested exchange symbol and natural key
- **THEN** the CLI MUST use the resolved `InstrumentId` to build `SeriesDescriptor(InstrumentId, TimeFrameCode.Day1)` for the simulation

#### Scenario: Symbol missing from registry

- **WHEN** the registry does not contain the requested exchange symbol
- **THEN** the CLI MUST exit with a non-zero status and an error message that instructs the operator to run `backfill-1d` (or otherwise load instruments) before retrying

### Requirement: Day1 series read from persisted candles

The backtest command SHALL load historical bars only through `ICandleSeriesReader.ReadAsync` for `TimeFrameCode.Day1`. It MUST NOT write synthetic bars or call Binance REST for klines during the backtest command.

#### Scenario: Bars available in range

- **WHEN** the candles store contains one or more `Day1` bars for the resolved series within the optional `--from` / `--to` range
- **THEN** the CLI MUST pass those bars to `IBacktestRunner` in chronological order (as returned by the reader)

#### Scenario: No bars in range

- **WHEN** the reader returns zero bars for the resolved series and range
- **THEN** the CLI MUST exit with a non-zero status and a message indicating missing `Day1` data or an empty date filter

### Requirement: FixedWindow strategy parameters

For strategy kind `FixedWindow`, the CLI SHALL construct a `TradingVectorSpec` with parameters `enterBar` and `exitBar` (0-based bar indices) supplied by the operator (`--enter-bar`, `--exit-bar`). The factory MUST resolve this to the existing `FixedWindowStrategy` implementation.

#### Scenario: FixedWindow run completes

- **WHEN** the operator specifies `--strategy FixedWindow` with valid enter/exit bar indices and bars exist
- **THEN** the simulation MUST produce a `SimulationRunResult` including trades, equity series, final equity, and max drawdown fraction

#### Scenario: Unsupported strategy kind

- **WHEN** the operator specifies a strategy kind other than `FixedWindow`
- **THEN** the CLI MUST exit with a non-zero status before starting the simulation

### Requirement: Simulation configuration flags

The CLI SHALL accept optional simulation configuration: initial capital, fee basis points per side, and position notional fraction of initial capital. When omitted, defaults MUST match the values used by the existing `demo` command for equivalent fields.

#### Scenario: Custom capital and fees

- **WHEN** the operator passes `--initial-capital`, `--fee-bps`, and/or `--position-fraction`
- **THEN** the CLI MUST build `SimulationConfiguration` from those values and include it in `BacktestRequest`

### Requirement: Run summary output

On successful completion, the CLI SHALL print at minimum: `RunId`, total trade count, final equity, and max drawdown as a fraction. Output MUST be suitable for manual verification in a terminal session.

#### Scenario: Summary printed after run

- **WHEN** the simulation completes with one or more bars processed
- **THEN** stdout MUST include the run identifier, trade count, final equity, and max drawdown fraction

### Requirement: Optional persistence of simulation runs

When the operator passes `--save`, the CLI SHALL persist the `SimulationRunResult` through `ISimulationRunRepository` to the configured research database path.

#### Scenario: Save flag persists result

- **WHEN** the operator includes `--save` and the simulation succeeds
- **THEN** the result MUST be written via `ISimulationRunRepository.SaveAsync` and the CLI MUST log the research database path used

#### Scenario: No save flag

- **WHEN** the operator omits `--save`
- **THEN** the CLI MUST NOT require a writable research database for a successful run (read-only market DB suffices)

### Requirement: Documented operator workflow

Platform documentation SHALL describe the two-step workflow: run `backfill-1d` to populate `market.sqlite`, then run `backtest` for a target symbol with `Day1` data.

#### Scenario: README workflow

- **WHEN** an operator consults `src/platform/TradingPlatform/README.md`
- **THEN** it MUST document the `backtest` command, its required flags, and the dependency on prior `backfill-1d` (or equivalent data load)
