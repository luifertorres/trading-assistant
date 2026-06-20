namespace TradingAssistant.Application;

public interface IClock
{
    DateTime UtcNow { get; }
}


