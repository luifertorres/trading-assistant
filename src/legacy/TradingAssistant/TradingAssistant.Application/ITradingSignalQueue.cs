namespace TradingAssistant.Application;

public interface ITradingSignalQueue
{
    void Enqueue(TradingSignalNotification signal);
    bool TryDequeue(out TradingSignalNotification signal);
    void Clear();
}


