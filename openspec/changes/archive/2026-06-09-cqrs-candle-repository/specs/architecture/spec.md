## MODIFIED Requirements

### Requirement: Clean Architecture Layer Separation

The system SHALL be organized into four layers per deployable service: Domain, Application, Infrastructure, and Host. The main trading app and the Candlestick Data API SHALL each enforce strict dependency direction internally and integrate across process boundaries through explicit service contracts.

#### Scenario: Candlestick service respects layer boundaries

- **WHEN** code is added to the Candlestick Data API Domain
- **THEN** it must not reference that service's Application, Infrastructure, or Host projects
- **AND** broker-specific concerns are implemented in Infrastructure through implementations of interfaces defined in Domain or Application

#### Scenario: Cross-service integration contract

- **WHEN** the main trading application needs candlestick synchronization or retrieval
- **THEN** it interacts with the Candlestick Data API through versioned API contracts
- **AND** it does not bypass the service by directly using the candlestick service persistence internals

### Requirement: Broker-Agnostic Domain

The domain model SHALL be independent of any specific broker or exchange implementation, including the candlestick bounded context.

#### Scenario: Candlestick bounded context remains broker-agnostic

- **WHEN** candlestick entities, value objects, and domain services are defined
- **THEN** they use domain-owned abstractions and types instead of Binance.Net-specific models
- **AND** exchange-specific mappings are isolated in Infrastructure implementations behind Domain/Application interfaces
