## ADDED Requirements

### Requirement: Single Symbol Exposure

The system SHALL enforce that only one symbol has an active position at any time.

#### Scenario: New entry with no active position

- **WHEN** `EntryRequested` is received and no position is active
- **THEN** `ExposureGuard` passes through and publishes `OrderSizingRequested`
- **AND** records the symbol as the active symbol

#### Scenario: Entry on same active symbol

- **WHEN** `EntryRequested` is received for the same symbol as the active position
- **THEN** `ExposureGuard` converts it to `RebuyRequested`
- **AND** the flow continues through `RebuyPolicy`

#### Scenario: Entry on different symbol rejected

- **WHEN** `EntryRequested` is received for a different symbol than the active position
- **THEN** `ExposureGuard` rejects the entry (logs and discards)
- **AND** no further events are published

#### Scenario: Symbol released on position close

- **WHEN** `PositionClosed` event is received
- **THEN** `ExposureGuard` clears the active symbol
- **AND** new entries for any symbol are allowed again
