using MarketData.Application;

namespace MarketData.Infrastructure;

internal static class BrokerFetchHandleUnwrap
{
    public static string Symbol(BrokerFetchHandle handle) =>
        handle.Token as string ?? throw new InvalidOperationException("Invalid broker fetch handle token.");
}
