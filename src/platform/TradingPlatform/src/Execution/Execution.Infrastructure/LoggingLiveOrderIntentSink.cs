using Execution.Application;
using Microsoft.Extensions.Logging;
using TradingPlatform.Kernel;

namespace Execution.Infrastructure;

public sealed class LoggingLiveOrderIntentSink(ILogger<LoggingLiveOrderIntentSink> log) : ILiveOrderIntentSink
{
    public Task OnIntentAsync(OrderIntent intent, CancellationToken cancellationToken = default)
    {
        log.LogInformation(
            "Live sink (stub ACL): {Kind} qty={Qty} tag={Tag}",
            intent.Kind,
            intent.Quantity,
            intent.Tag);
        return Task.CompletedTask;
    }
}
