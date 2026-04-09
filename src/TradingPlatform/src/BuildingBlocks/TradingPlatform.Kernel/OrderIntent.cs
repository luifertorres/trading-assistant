namespace TradingPlatform.Kernel;

public enum OrderIntentKind
{
    OpenLong,
    OpenShort,
    ClosePosition
}

/// <summary>Strategy output: execution/simulation maps this to fills or live orders.</summary>
public readonly record struct OrderIntent(OrderIntentKind Kind, decimal Quantity, string? Tag);
