namespace CandlestickData.Domain;

public enum IntegrityReason
{
    None,
    NormalGap,
    AtypicalGap,
    Remediating,
    Unknown
}
