using Microsoft.Extensions.Logging;

namespace Execution.Infrastructure;

/// <summary>Placeholder for a future Binance (or other) adapter—keeps exchange types out of Domain/Application.</summary>
public sealed class BrokerAntiCorruptionStub(ILogger<BrokerAntiCorruptionStub> log)
{
    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        log.LogInformation("Broker ACL stub: no external connection.");
        return Task.CompletedTask;
    }
}
