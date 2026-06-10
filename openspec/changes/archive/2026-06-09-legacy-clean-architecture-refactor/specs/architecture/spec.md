## MODIFIED Requirements

### Requirement: Clean Architecture Layer Separation

#### Scenario: Handlers live in Application layer

- **WHEN** a MediatR handler or strategy is added
- **THEN** it must be placed in `TradingAssistant.Application` (not Host)
- **AND** the Host assembly no longer needs its own MediatR scan

### Requirement: Broker-Agnostic Domain

#### Scenario: Domain-owned TimeFrame enum

- **WHEN** a candle timeframe is referenced in the domain
- **THEN** it uses `TimeFrame` enum (domain-owned), not `KlineInterval` (Binance.Net)

#### Scenario: Domain-owned OrderSide enum

- **WHEN** an order direction is referenced in the domain
- **THEN** it uses `OrderSide` enum (domain-owned), not Binance.Net types

## ADDED Requirements

### Requirement: Type Mapping in Infrastructure

Infrastructure adapters SHALL map between domain-owned types and broker-specific types via dedicated mapper classes.

#### Scenario: TimeFrame mapping

- **WHEN** infrastructure code needs to convert between domain and Binance types
- **THEN** `BinanceTypeMapper.ToDomain(KlineInterval)` returns the corresponding `TimeFrame`
- **AND** `BinanceTypeMapper.ToBinance(TimeFrame)` returns the corresponding `KlineInterval`

#### Scenario: OrderSide mapping

- **WHEN** infrastructure code needs to convert order directions
- **THEN** `BinanceTypeMapper` maps between domain `OrderSide` and Binance `OrderSide`
