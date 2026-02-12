## ADDED Requirements

### Requirement: Event Bus Abstraction

The system SHALL provide an in-process event bus for publishing and subscribing to domain events.

#### Scenario: Event published and handled

- **WHEN** a service publishes an event via `IEventBus.PublishAsync<TEvent>(event)`
- **THEN** all registered handlers for that event type are invoked
- **AND** handlers execute asynchronously with cancellation support

#### Scenario: MediatR backend

- **WHEN** the event bus is implemented
- **THEN** `MediatREventBus` wraps MediatR's `IPublisher` to dispatch events as notifications
- **AND** domain event records implement both `IDomainEvent` and MediatR's `INotification`

### Requirement: Domain Event Contracts

Domain events SHALL be defined as immutable records in `TradingAssistant.Domain.Events`.

#### Scenario: Event immutability

- **WHEN** a domain event is created
- **THEN** it is an immutable record type
- **AND** it includes a timestamp and correlation ID for tracing

### Requirement: Event Persistence

The system SHALL optionally persist events to an `EventLog` table for debugging and replay.

#### Scenario: Event logged

- **WHEN** an event is published through the event bus
- **THEN** it is serialized and stored in the `EventLog` table with timestamp, event type, and payload
- **AND** logging can be enabled/disabled via configuration
