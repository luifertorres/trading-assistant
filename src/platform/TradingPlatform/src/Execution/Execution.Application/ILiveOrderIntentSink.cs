using TradingPlatform.Kernel;

namespace Execution.Application;

/// <summary>Live (or paper) path: maps the same <see cref="OrderIntent"/> as simulation to broker I/O.</summary>
public interface ILiveOrderIntentSink
{
    Task OnIntentAsync(OrderIntent intent, CancellationToken cancellationToken = default);
}
