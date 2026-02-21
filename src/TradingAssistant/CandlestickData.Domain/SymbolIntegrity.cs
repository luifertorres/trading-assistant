namespace CandlestickData.Domain;

public class SymbolIntegrity
{
    public required string Symbol { get; init; }
    public required TimeFrame TimeFrame { get; init; }
    public IntegrityStatus Status { get; private set; }
    public IntegrityReason Reason { get; private set; }
    public DateTime? GapFromOpenTime { get; private set; }
    public DateTime? GapToOpenTime { get; private set; }
    public DateTime? LastVerifiedOpenTime { get; private set; }
    public DateTime? DetectedAt { get; private set; }

    public void MarkEligible(DateTime lastVerifiedOpenTime, DateTime now)
    {
        Status = IntegrityStatus.Eligible;
        Reason = IntegrityReason.None;
        GapFromOpenTime = null;
        GapToOpenTime = null;
        LastVerifiedOpenTime = lastVerifiedOpenTime;
        DetectedAt = now;
    }

    public void MarkCompromised(IntegrityReason reason, DateTime gapFrom, DateTime gapTo, DateTime now)
    {
        Status = IntegrityStatus.Compromised;
        Reason = reason;
        GapFromOpenTime = gapFrom;
        GapToOpenTime = gapTo;
        DetectedAt = now;
    }

    public void MarkRecovering(DateTime now)
    {
        Status = IntegrityStatus.Recovering;
        Reason = IntegrityReason.Remediating;
        DetectedAt = now;
    }

    public bool IsEligible => Status == IntegrityStatus.Eligible;

    public static SymbolIntegrity CreateEligible(string symbol, TimeFrame timeFrame, DateTime now) => new()
    {
        Symbol = symbol,
        TimeFrame = timeFrame,
        Status = IntegrityStatus.Eligible,
        Reason = IntegrityReason.None,
        DetectedAt = now
    };
}
