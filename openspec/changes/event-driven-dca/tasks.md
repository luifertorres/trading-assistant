## 1. Event Infrastructure

- [ ] 1.1 Create `IDomainEvent` marker interface in Domain
- [ ] 1.2 Create `IEventBus` interface in Application (PublishAsync, Subscribe)
- [ ] 1.3 Create event record types in `Domain/Events/`: CandleClosed, IndicatorCalculated, IndicatorThresholdCrossed
- [ ] 1.4 Create strategy intent events: EntryRequested, RebuyRequested, ExitRequested
- [ ] 1.5 Create account events: AccountSnapshotUpdated, MarginReserved, MarginReleased, MarginDepleted
- [ ] 1.6 Create sizing events: OrderSizingRequested, OrderSizingApproved, OrderPlacementRequested, OrderFilled, OrderRejected
- [ ] 1.7 Create position events: PositionOpened, PositionAugmented, PositionClosed, PositionFailed
- [ ] 1.8 Implement `MediatREventBus` in Infrastructure wrapping `IPublisher`
- [ ] 1.9 Register `IEventBus` in DI

## 2. Refactor Signal Flow

- [ ] 2.1 Modify `Rsi5ExtremeStrategy` to publish `EntryRequested` instead of `TradingSignalNotification`
- [ ] 2.2 Create `ExposureGuard` service (subscribes to EntryRequested, maintains ActiveSymbol)
- [ ] 2.3 Replace `TradingSignalWorker` with `EventBusWorker` for pumping external events
- [ ] 2.4 Verify RSI(5) signal flow works end-to-end with new events

## 3. Split TradeHandler

- [ ] 3.1 Create `CapitalAllocator` — subscribes to OrderSizingRequested, publishes OrderSizingApproved + MarginReserved
- [ ] 3.2 Create `ExecutionOrchestrator` — subscribes to OrderPlacementRequested, calls IExchangeService, publishes OrderFilled/OrderRejected
- [ ] 3.3 Create `MarginSupervisor` — subscribes to OrderFilled + PositionClosed, tracks committed margin
- [ ] 3.4 Create `PositionProjector` — subscribes to OrderFilled, updates position state, publishes PositionOpened/Augmented/Closed
- [ ] 3.5 Adapt `IExchangeService` for orders without SL/TP/TSL parameters
- [ ] 3.6 Wire all new services in DI

## 4. Persistence

- [ ] 4.1 Create `Positions` table schema (symbol, blocks, average price, margin reserved)
- [ ] 4.2 Create `EventLog` table schema (timestamp, event type, payload JSON)
- [ ] 4.3 Generate EF Core migration
- [ ] 4.4 Implement position persistence in `PositionProjector`

## 5. DCA Validation

- [ ] 5.1 Create `RebuyPolicy` service — validates RSI + bearish candle + balance >= block
- [ ] 5.2 Implement block sizing: max(5% balance, exchange minimum)
- [ ] 5.3 Wire `RebuyPolicy` to subscribe to `RebuyRequested`
- [ ] 5.4 Verify full DCA rebuy cycle: entry → rebuy → rebuy → exit

## 6. Tests

- [ ] 6.1 Unit test: ExposureGuard — rejects different symbol, converts same symbol to rebuy
- [ ] 6.2 Unit test: CapitalAllocator — 5% block sizing, rejects when margin depleted
- [ ] 6.3 Unit test: MarginSupervisor — tracks margin, publishes MarginDepleted
- [ ] 6.4 Unit test: PositionProjector — calculates average price after rebuys
- [ ] 6.5 Unit test: RebuyPolicy — validates conditions
- [ ] 6.6 Integration test: Full RSI(5) DCA flow with in-memory event bus

## 7. Strategy Migration (optional, later)

- [ ] 7.1 Port MeanReversion strategies to event-driven pattern
- [ ] 7.2 Port TrendFollowing strategy
- [ ] 7.3 Share ExposureGuard and CapitalAllocator across all strategies
