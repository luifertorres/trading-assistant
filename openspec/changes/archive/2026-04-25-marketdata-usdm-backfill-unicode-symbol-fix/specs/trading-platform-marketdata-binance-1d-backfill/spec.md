## ADDED Requirements

### Requirement: Full exchange symbol alphabet

The backfill SHALL accept every symbol returned by Binance USD-M `exchangeInfo` that already satisfies the universe filter (`status = TRADING`, `contractType = PERPETUAL`, `quoteAsset = USDT`), regardless of the symbol's character set. The implementation MUST NOT reject an otherwise-qualifying symbol on character-class grounds alone (for example, because it contains non-ASCII letters, Unicode code points outside `[A-Za-z0-9]`, or a leading digit). Any safety invariants on characters that are actually unsafe in the storage layer (for example, ASCII control characters or characters that would break an SQL quoted identifier such as `"`) SHALL be enforced at the storage boundary, not by widening the list of disallowed symbols, and MUST always allow the full set of symbols the exchange currently lists as `TRADING` `PERPETUAL` `USDT`.

#### Scenario: Unicode symbols are accepted

- **WHEN** Binance USD-M `exchangeInfo` lists a `TRADING` `PERPETUAL` `USDT` contract whose `symbol` contains non-ASCII characters (for example `龙虾USDT`, `币安人生USDT`, `我踏马来了USDT`)
- **THEN** the backfill MUST include that symbol in the run universe, derive a `SeriesDescriptor` for it, and persist its daily bars through `ICandleSeriesWriter.UpsertAsync` without throwing any character-class validation error for the symbol

#### Scenario: Digit-prefixed symbols are accepted

- **WHEN** Binance USD-M `exchangeInfo` lists a `TRADING` `PERPETUAL` `USDT` contract whose `symbol` starts with one or more ASCII digits (for example `1INCHUSDT`, `1000PEPEUSDT`, `1000000MOGUSDT`, `4USDT`)
- **THEN** the backfill MUST include that symbol in the run universe, derive a `SeriesDescriptor` for it, and persist its daily bars through `ICandleSeriesWriter.UpsertAsync` without depending on any character-class rule that would disallow digit-prefixed names

#### Scenario: Unsafe storage characters are rejected at the storage boundary only

- **WHEN** a symbol passed to the storage layer contains an ASCII control character (`U+0000`–`U+001F`, `U+007F`) or a literal double-quote character (`"`) that would break the SQL quoted-identifier form
- **THEN** the storage layer MUST refuse that symbol with a clear error; no such character set MAY be enforced by the domain in a way that would also reject Unicode letters, digit-prefixed names, or other characters that the exchange actually uses

### Requirement: Per-symbol failure isolation

A single symbol's failure SHALL NOT abort the remainder of a backfill run. When the orchestrator processes the universe in a single run, any exception thrown while handling one symbol (including failures in series-name derivation, checkpoint update, exchange calls, or the candle writer) MUST be caught at symbol scope, logged with the offending symbol and a human-readable error, and recorded on that symbol's checkpoint entry so operators can inspect which symbols succeeded and which failed. The orchestrator MUST then continue with the next symbol in the universe and MUST return normally to its caller if all remaining symbols complete (successfully or with their own recorded failures).

#### Scenario: One failing symbol does not abort the run

- **WHEN** a backfill run iterates the symbol universe and processing one symbol throws an exception
- **THEN** the orchestrator MUST log the failure, record it on that symbol's checkpoint entry, and continue with the next symbol rather than propagating the exception out of the run

#### Scenario: Failure is observable on the checkpoint

- **WHEN** a symbol has failed at least once during the current run
- **THEN** the checkpoint entry for that symbol MUST carry enough information for an operator to identify the failure (at minimum: the error message and a UTC timestamp of the most recent failure), and the symbol MUST NOT be marked `complete` unless its backfill later succeeds

#### Scenario: Healthy symbols still complete normally

- **WHEN** a run contains a mix of failing and healthy symbols
- **THEN** every healthy symbol MUST be processed to completion and have its bars persisted, regardless of the position of failing symbols in the iteration order

