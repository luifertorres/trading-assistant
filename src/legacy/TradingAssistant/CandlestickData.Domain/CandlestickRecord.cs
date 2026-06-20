namespace CandlestickData.Domain;

public class CandlestickRecord
{
    public required string Symbol { get; init; }
    public required TimeFrame TimeFrame { get; init; }
    public required DateTime OpenTime { get; init; }
    public required DateTime CloseTime { get; init; }
    public required decimal OpenPrice { get; init; }
    public required decimal HighPrice { get; init; }
    public required decimal LowPrice { get; init; }
    public required decimal ClosePrice { get; init; }
    public required decimal Volume { get; init; }

    public bool IsValid()
    {
        return HighPrice >= LowPrice
            && HighPrice >= OpenPrice
            && HighPrice >= ClosePrice
            && LowPrice <= OpenPrice
            && LowPrice <= ClosePrice
            && Volume >= 0
            && CloseTime > OpenTime;
    }
}
