namespace Research.Application;

public interface ITradingStrategy
{
    void OnBar(BarProcessingContext context);
}
