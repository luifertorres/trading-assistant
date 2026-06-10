## MODIFIED Requirements

### Requirement: Candle Data Model

#### Scenario: CandleId uses domain TimeFrame

- **WHEN** a `CandleId` is constructed
- **THEN** it uses `TimeFrame` (domain-owned enum) instead of `KlineInterval` (Binance.Net)
- **AND** all code referencing `CandleId.TimeFrame` uses the domain type

## ADDED Requirements

### Requirement: Domain-Owned Enums

The domain SHALL define its own trading enums independent of any broker library.

#### Scenario: TimeFrame enum defined

- **WHEN** code needs to represent a candle interval
- **THEN** `TimeFrame` enum is available in Domain with values: OneMinute, FiveMinutes, FifteenMinutes, ThirtyMinutes, OneHour, FourHours, OneDay, OneWeek, OneMonth

#### Scenario: OrderSide enum defined

- **WHEN** code needs to represent a trade direction
- **THEN** `OrderSide` enum is available in Domain with values: Buy, Sell

#### Scenario: PositionSide enum defined

- **WHEN** code needs to represent a position direction
- **THEN** `PositionSide` enum is available in Domain with values: Long, Short
