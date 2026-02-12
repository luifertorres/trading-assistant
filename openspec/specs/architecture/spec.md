## Purpose

Defines the architectural constraints, layer responsibilities, and dependency rules for the trading-assistant system. All other capabilities must conform to these rules.

## Requirements

### Requirement: Clean Architecture Layer Separation

The system SHALL be organized into four layers: Domain, Application, Infrastructure, and Host. Each layer has a strict dependency direction that must not be violated.

#### Scenario: Domain layer independence

- **WHEN** code is added to `TradingAssistant.Domain`
- **THEN** it must not reference Application, Infrastructure, or Host projects
- **AND** it must not reference infrastructure NuGet packages (EF Core, Binance.Net, FASTER, HTTP clients)

#### Scenario: Application layer references

- **WHEN** code is added to `TradingAssistant.Application`
- **THEN** it may only reference `TradingAssistant.Domain`
- **AND** it must not reference Infrastructure or Host projects
- **AND** it must not reference infrastructure NuGet packages

#### Scenario: Infrastructure layer references

- **WHEN** code is added to `TradingAssistant.Infrastructure`
- **THEN** it may reference `TradingAssistant.Application` and `TradingAssistant.Domain`
- **AND** it must not reference the Host project

#### Scenario: Host layer as composition root

- **WHEN** code is added to `TradingAssistant` (Host)
- **THEN** it may reference Application and Infrastructure
- **AND** it should only contain DI registration, BackgroundService orchestration, and configuration

### Requirement: DI Registration Pattern

Dependency injection SHALL be organized with one extension method per layer, invoked from the Host's `Program.cs`.

#### Scenario: Service registration

- **WHEN** the application starts
- **THEN** `AddApplication()` registers MediatR from the Application assembly
- **AND** `AddInfrastructure(IConfiguration)` registers all infrastructure services (EF Core, FASTER, queues, adapters)
- **AND** `AddBinance(Action<BinanceRestOptions>)` registers Binance-specific services
- **AND** hosted services are registered individually via `AddHostedService<T>()`

### Requirement: Rich Domain Model

The domain layer SHALL use Domain-Driven Design with a rich domain model. Entities must encapsulate behavior and enforce invariants.

#### Scenario: Entity with behavior

- **WHEN** a domain entity is created or modified
- **THEN** it must contain methods that enforce business rules
- **AND** it must not be an anemic model (data-only with external logic)

#### Scenario: Value Object immutability

- **WHEN** a value object is defined
- **THEN** it must be immutable (using `record`, `readonly record struct`, or `readonly struct`)
- **AND** it must validate its state at construction

### Requirement: Broker-Agnostic Domain

The domain model SHALL be independent of any specific broker or exchange implementation.

#### Scenario: No broker types in domain

- **WHEN** domain entities or value objects are defined
- **THEN** they must use domain-owned types (not Binance.Net types like `KlineInterval`)
- **AND** broker-specific type mapping must happen in Infrastructure adapters (Anti-Corruption Layer)

### Requirement: MediatR Messaging

In-process communication between layers SHALL use MediatR notifications and requests.

#### Scenario: Notification for events

- **WHEN** a domain event occurs (candle closed, indicator calculated, signal generated)
- **THEN** it is published as an `INotification` with multiple possible handlers

#### Scenario: Request for commands

- **WHEN** a command needs a single handler with a result (e.g., place trade)
- **THEN** it is sent as an `IRequest<T>` with exactly one handler

### Requirement: Migrations Policy

Database migrations SHALL be managed through EF Core CLI commands only, never edited manually.

#### Scenario: Creating a migration

- **WHEN** a schema change is needed
- **THEN** run `dotnet ef migrations add <Name> --project TradingAssistant.Infrastructure --startup-project TradingAssistant --output-dir Migrations`
- **AND** never manually edit the generated migration files
