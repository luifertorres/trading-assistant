## 1. Series Table Naming

[x] 1.1 Update `SeriesTableNaming.ToPhysicalTableName` so symbol validation rejects only empty symbols, ASCII control characters, and literal double-quote characters, while preserving current upper-casing and timeframe normalization.
[x] 1.2 Update `SeriesTableNamingTests` to accept `BTCUSDT`, `龙虾USDT`, `币安人生USDT`, `我踏马来了USDT`, `1000PEPEUSDT`, and `4USDT`.
[x] 1.3 Update `SeriesTableNamingTests` to reject empty symbols, symbols containing `"` characters, and symbols containing ASCII control characters.

## 2. Checkpoint Failure Metadata

[x] 2.1 Add nullable `LastErrorMessage` and `LastErrorAtUtc` fields to the per-symbol checkpoint entry without changing checkpoint schema version.
[x] 2.2 Ensure checkpoint serialization omits the new error fields when they are `null` and deserializes existing v1 checkpoint files with both fields defaulting to `null`.
[x] 2.3 Clear `LastErrorMessage` and `LastErrorAtUtc` when a previously failing symbol later completes successfully.

## 3. Orchestrator Failure Isolation

[x] 3.1 Wrap `Usdm1dBackfillOrchestrator` symbol processing in a symbol-scoped `try/catch` that logs recoverable failures at `Warning`, annotates the symbol checkpoint entry, saves the checkpoint, and continues with the next symbol.
[x] 3.2 Ensure `OperationCanceledException` is not swallowed by symbol-level failure handling and still terminates the run promptly.
[x] 3.3 Ensure failed symbols are not marked `Complete = true` unless a later successful pass reaches the existing completion path.

## 4. Application Tests

[x] 4.1 Add or extend orchestrator tests with a fake exchange/writer where one symbol fails and a later healthy symbol still writes bars and completes.
[x] 4.2 Assert the failed symbol's checkpoint entry contains `LastErrorMessage` and `LastErrorAtUtc`, and remains incomplete.
[x] 4.3 Assert cancellation still propagates out of `RunAsync` and is not recorded as a per-symbol failure.
[x] 4.4 Add a checkpoint compatibility test that loads a pre-change v1 checkpoint fixture and verifies missing error fields default to `null`.

## 5. Documentation

[x] 5.1 Add a short ADR entry in `src/TradingPlatform/docs/ADRs.md` documenting the temporary raw-symbol storage-name approach, the relaxed predicate, and the follow-up `marketdata-instrument-identity-and-candles-registry` change.

## 6. Verification

[x] 6.1 Run the focused MarketData domain tests and application tests that cover this change.
[x] 6.2 Run OpenSpec validation for `marketdata-usdm-backfill-unicode-symbol-fix`.