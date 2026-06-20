namespace Backtesting.Mvp;

public sealed record TradeRecord(
    DateTime EntryTime,
    DateTime ExitTime,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal Quantity,
    decimal GrossPnl,
    decimal Fees,
    decimal NetPnl);
