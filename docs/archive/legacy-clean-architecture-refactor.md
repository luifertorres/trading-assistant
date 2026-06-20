> **Status: archived.** See [archive README](README.md) for current guidance.

Clean Architecture Refactor Plan (Multi-project, Worker host)

Solution layout
- src/legacy/TradingAssistant/TradingAssistant.Domain
- src/legacy/TradingAssistant/TradingAssistant.Application
- src/legacy/TradingAssistant/TradingAssistant.Infrastructure
- src/legacy/TradingAssistant/TradingAssistant (current host)

Layer dependencies
- Application -> Domain
- Infrastructure -> Application, Domain
- Host -> Application, Infrastructure

Status (done)
- Domain
  - Moved core domain/value objects and utilities: Length, NumberExtensions, Rsi, CircularTimeSeries, CandlestickGap, StopLossPrice, TakeProfitPrice, SteppedTrailingStop.
  - Moved market primitives: Candle, CandleId, CandlestickId.
- Application
  - Moved MediatR notifications: CandleClosedNotification, SmasAndRsisCalculatedEvent, TradingSignalNotification.
  - Added interfaces: IExchangeService, ICandleRepository, ITradingSignalQueue, IClock.
- Infrastructure
  - EF Core: moved TradingContext; added TradingContextFactory for design-time; regenerated migrations under Infrastructure via dotnet-ef; database updated.
  - FASTER: added serializers (CandleIdSerializer, CandleSerializer) and FasterCandleRepository; DI wired.
  - Queue: implemented TradingSignalQueue (ITradingSignalQueue); DI wired.
  - Binance: moved BinanceService into Infrastructure (namespace kept as TradingAssistant); published BinanceExtensions; added adapter BinanceExchangeService implementing IExchangeService; DI wired (BinanceService + IExchangeService).
- Host (current)
  - Program.cs simplified to AddApplication() + AddInfrastructure(); leaves AddBinance credential wiring; hosted services intact.
  - Managers (StopLoss/TakeProfit/SteppedTrailing/TrailingStop) depend on IExchangeService.
- Verified: build succeeds and live run shows no errors.

What remains (next steps)
1) Application/Handlers
   - Move remaining strategy/handler classes (MeanReversion*, TrendFollowing*, TradeHandler, RsiCandleClosedHandler) fully into Application where they handle notifications.
   - Replace direct FASTER usage with ICandleRepository in handlers (e.g., RsiCandleClosedHandler, TradeHandler).
2) Exchange decoupling
   - Expand IExchangeService for any residual operations needed by TradeHandler; remove remaining direct BinanceService references.
3) Host split (optional, later)
   - Introduce TradingAssistant.Worker as the sole host project and move Program.cs/config there; leave current host temporarily for migration.
4) Config and ops
   - Move appsettings.* and Dockerfile to Worker; keep EF design-time factory in Infrastructure.
   - Ensure no Infrastructure package references appear in Domain/Application; keep MediatR only in Application.

DI summary
- In Application: AddApplication() registers MediatR from Application assembly.
- In Infrastructure: AddInfrastructure(configuration)
  - AddDbContext<TradingContext>()
  - Add FASTER store + ICandleRepository
  - Add TradingSignalQueue (ITradingSignalQueue)
  - Add BinanceService and IExchangeService (BinanceExchangeService)

Migrations policy
- Never edit migration files manually.
- Use dotnet-ef from src/legacy/TradingAssistant:
  - dotnet ef migrations add <Name> --project TradingAssistant.Infrastructure --startup-project TradingAssistant --output-dir Migrations
  - dotnet ef database update --project TradingAssistant.Infrastructure --startup-project TradingAssistant

Acceptance criteria
- Build and run succeed from host; background services process signals.
- Domain/Application remain infrastructure-agnostic.
- All consumers use IExchangeService/ICandleRepository; no direct FASTER/Binance in Application/Host code beyond DI.


