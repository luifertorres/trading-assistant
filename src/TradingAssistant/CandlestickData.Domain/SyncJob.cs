namespace CandlestickData.Domain;

public class SyncJob
{
    public Guid Id { get; private set; }
    public SyncJobState State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public bool IsRunning => State == SyncJobState.Running;

    public void Start(DateTime now)
    {
        State = SyncJobState.Running;
        StartedAt = now;
    }

    public void Stop(DateTime now)
    {
        State = SyncJobState.Stopped;
        StoppedAt = now;
    }

    public void Complete(DateTime now)
    {
        State = SyncJobState.Completed;
        CompletedAt = now;
    }

    public void Fail(string reason, DateTime now)
    {
        State = SyncJobState.Failed;
        FailureReason = reason;
        StoppedAt = now;
    }

    public static SyncJob Create(DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        State = SyncJobState.Idle,
        CreatedAt = now
    };
}
