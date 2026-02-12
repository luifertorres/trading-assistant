## 1. Domain Types

- [ ] 1.1 Create `BacktestStatus` enum: Pending, Running, Completed, Failed
- [ ] 1.2 Create `ExitReason` enum: StopLoss, TakeProfit, Signal, EndOfData
- [ ] 1.3 Create `BacktestConfig` record: InitialBalance, Leverage, CommissionRate, SlippageBps, RiskConfig
- [ ] 1.4 Create `EquityPoint` record: Timestamp, Equity
- [ ] 1.5 Create `BacktestTrade` record: Symbol, Side, EntryPrice, ExitPrice, EntryTime, ExitTime, Quantity, PnL, PnLPercentage, ExitReason
- [ ] 1.6 Create `BacktestResult` record: TotalTrades, WinRate, NetProfit, MaxDrawdown, SharpeRatio, ProfitFactor, AverageWin, AverageLoss, MaxConsecutiveLosses, EquityCurve, Trades
- [ ] 1.7 Create `BacktestRun` entity (aggregate root): Id, StrategyName, Symbol, TimeFrame, StartDate, EndDate, Status, Config, Result, CreatedAt
- [ ] 1.8 Add behavior methods to `BacktestRun`: `Start()`, `Complete(BacktestResult)`, `Fail(string reason)`

## 2. Strategy Abstraction

- [ ] 2.1 Create `ITradingStrategy` interface in Application: Name, RiskProfile, OnCandle(Candle, IStrategyContext)
- [ ] 2.2 Create `IStrategyContext` interface in Application: HasOpenPosition, CurrentBalance, RequestEntry(), RequestExit()
- [ ] 2.3 Create adapter for `Rsi5ExtremeStrategy` implementing `ITradingStrategy`
- [ ] 2.4 Verify adapter correctly translates RSI(5) < 10 + bearish candle logic

## 3. Application Interfaces

- [ ] 3.1 Create `IBacktestEngine` interface: RunAsync(BacktestRun, CancellationToken) → BacktestResult
- [ ] 3.2 Create `IHistoricalDataProvider` interface: GetCandlesAsync(symbol, timeFrame, from, to, ct) → IReadOnlyList<Candle>
- [ ] 3.3 Create `ISimulatedExchange` interface: PlaceOrder(), GetClosedTrades(), GetEquity()
- [ ] 3.4 Create `IBacktestRunRepository` interface: SaveAsync(), GetByIdAsync(), GetByStrategyAsync()

## 4. Infrastructure Implementation

- [ ] 4.1 Implement `SimulatedExchange` — in-memory order tracking with OHLC fill logic
- [ ] 4.2 Implement `BacktestEngine` — candle iteration loop, strategy invocation, result calculation
- [ ] 4.3 Implement metric calculations: WinRate, SharpeRatio, MaxDrawdown, ProfitFactor
- [ ] 4.4 Implement `HistoricalDataProvider` — fetch from exchange REST API with FASTER caching
- [ ] 4.5 Add `BacktestRuns` table to `TradingContext` DbContext
- [ ] 4.6 Generate EF Core migration for BacktestRuns table
- [ ] 4.7 Implement `BacktestRunRepository` using EF Core

## 5. CSV Export

- [ ] 5.1 Create `IBacktestExporter` interface in Application
- [ ] 5.2 Implement `CsvBacktestExporter` in Infrastructure — trade list + summary metrics

## 6. DI Registration

- [ ] 6.1 Register backtesting services in `AddInfrastructure()`
- [ ] 6.2 Register strategy adapters

## 7. Verification

- [ ] 7.1 Build succeeds across all projects
- [ ] 7.2 Unit test: `SimulatedExchange` correctly fills market orders at close price
- [ ] 7.3 Unit test: `BacktestEngine` processes candles in chronological order
- [ ] 7.4 Unit test: Metric calculations (WinRate, SharpeRatio, MaxDrawdown)
- [ ] 7.5 Integration test: Full backtest run with Rsi5ExtremeStrategy adapter
- [ ] 7.6 Performance test: 1 year of 1-minute data completes in < 5 seconds
