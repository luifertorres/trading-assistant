namespace CandlestickData.Domain;

public enum SyncJobState
{
    Idle,
    Running,
    Stopped,
    Completed,
    Failed
}
