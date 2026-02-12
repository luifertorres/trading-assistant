## ADDED Requirements

### Requirement: DCA Rebuy Flow

The system SHALL support multiple buy orders (rebuys) on the same symbol to average down the entry price.

#### Scenario: First entry on new symbol

- **WHEN** `Rsi5ExtremeStrategy` detects RSI(5) < 10 with a bearish candle and no active position
- **THEN** `EntryRequested` event is published
- **AND** the flow proceeds through sizing → execution → position opened

#### Scenario: Rebuy on same symbol

- **WHEN** `Rsi5ExtremeStrategy` detects conditions again and a position already exists on the same symbol
- **THEN** `RebuyRequested` event is published
- **AND** `RebuyPolicy` validates conditions (RSI still extreme, candle bearish, balance >= block)
- **AND** a new block is added to the existing position

#### Scenario: Average price updated after rebuy

- **WHEN** a rebuy order is filled
- **THEN** `PositionProjector` recalculates the average entry price
- **AND** publishes `PositionAugmented` with new average price and total quantity

### Requirement: Exit via RSI Recovery

The system SHALL support closing DCA positions when RSI(5) recovers above 90.

#### Scenario: RSI exit triggered

- **WHEN** RSI(5) rises above 90 for a symbol with an active DCA position
- **THEN** `ExitRequested` event is published
- **AND** `ExecutionOrchestrator` closes the entire position at market

### Requirement: Rebuy Policy Validation

The system SHALL validate rebuy conditions before approving a DCA rebuy.

#### Scenario: Valid rebuy conditions

- **WHEN** a `RebuyRequested` event is received
- **THEN** `RebuyPolicy` verifies RSI is still below threshold, candle is bearish, and balance >= block size
- **AND** publishes `OrderSizingRequested` if all conditions pass

#### Scenario: Invalid rebuy rejected

- **WHEN** a `RebuyRequested` event is received but conditions are not met
- **THEN** the rebuy is silently rejected (logged, no further events)
