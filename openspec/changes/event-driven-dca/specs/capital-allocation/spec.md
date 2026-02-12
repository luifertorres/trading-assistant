## ADDED Requirements

### Requirement: Block-Based Capital Allocation

The system SHALL allocate capital in fixed-percentage blocks with no leverage.

#### Scenario: Initial entry sizing

- **WHEN** `OrderSizingRequested` is received for a new entry
- **THEN** `CapitalAllocator` calculates block size as 5% of current balance
- **AND** sets leverage to 1 (no leverage)
- **AND** publishes `OrderSizingApproved` with quantity and `MarginReserved`

#### Scenario: Rebuy sizing

- **WHEN** `OrderSizingRequested` is received for a rebuy
- **THEN** `CapitalAllocator` calculates block size as max(5% of balance, exchange minimum)
- **AND** publishes `OrderSizingApproved` with leverage=1

### Requirement: Margin Tracking

The system SHALL track committed margin to prevent over-allocation.

#### Scenario: Margin reserved on order

- **WHEN** an order is approved by `CapitalAllocator`
- **THEN** `MarginSupervisor` adds the amount to committed margin tracking

#### Scenario: Margin released on position close

- **WHEN** a position is closed
- **THEN** `MarginSupervisor` releases the committed margin for that symbol
- **AND** publishes `MarginReleased`

#### Scenario: Margin depleted blocks new orders

- **WHEN** committed margin equals or exceeds available balance
- **THEN** `MarginSupervisor` publishes `MarginDepleted`
- **AND** `CapitalAllocator` rejects all new sizing requests until margin is released

### Requirement: No Leverage Enforcement

The system SHALL enforce leverage=1 on all orders placed through the DCA flow.

#### Scenario: Leverage set on order

- **WHEN** `OrderSizingApproved` is published
- **THEN** leverage is always set to 1
- **AND** `IExchangeService` uses 1x mode for the order
