## ADDED Requirements

### Requirement: Capital.com Exchange Adapter

The system SHALL provide a Capital.com adapter implementing `IExchangeService` and `IExchangeStreamService`.

#### Scenario: REST API connection

- **WHEN** Capital.com is configured as the active broker
- **THEN** `CapitalComExchangeService` connects to the Capital.com REST API
- **AND** uses the configured API key, password, and base URL

#### Scenario: Demo vs live mode

- **WHEN** `Exchange:CapitalCom:IsDemo` is `true`
- **THEN** the adapter connects to the Capital.com demo environment
- **AND** no real money is at risk

#### Scenario: Candle data fetched

- **WHEN** historical candles are requested
- **THEN** `CapitalComExchangeService.GetCandlesAsync()` returns domain `Candle` types
- **AND** Capital.com-specific DTOs are mapped via `CapitalComTypeMapper`

#### Scenario: WebSocket streaming

- **WHEN** real-time candle updates are needed
- **THEN** `CapitalComStreamService` connects to Capital.com's streaming API
- **AND** delivers candles through the `IExchangeStreamService` interface

### Requirement: Broker Selection via Configuration

The system SHALL support selecting the active broker through configuration without code changes.

#### Scenario: Binance selected

- **WHEN** `Exchange:ActiveBroker` is set to "Binance"
- **THEN** Binance adapter services are registered in DI
- **AND** Capital.com services are not registered

#### Scenario: Capital.com selected

- **WHEN** `Exchange:ActiveBroker` is set to "CapitalCom"
- **THEN** Capital.com adapter services are registered in DI
- **AND** Binance services are not registered

#### Scenario: Unknown broker

- **WHEN** `Exchange:ActiveBroker` is set to an unrecognized value
- **THEN** the application throws `InvalidOperationException` at startup with a clear message

### Requirement: Strategies Work Across Brokers

Trading strategies SHALL produce identical behavior regardless of which broker is active.

#### Scenario: Strategy agnostic of broker

- **WHEN** a strategy evaluates entry/exit conditions
- **THEN** it uses only domain types (Candle, TimeFrame, OrderSide) and Application interfaces
- **AND** it never references broker-specific types or configurations
