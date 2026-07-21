using Research.Application;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

/// <summary>Demo strategy: open once at EnterBar index, close at ExitBar. Parameters: enterBar, exitBar (0-based).</summary>
public sealed class FixedWindowStrategy : ITradingStrategy
{
    public void OnBar(BarProcessingContext context)
    {
        var enter = int.Parse(context.Vector.Parameters.GetValueOrDefault("enterBar", "5"));
        var exit = int.Parse(context.Vector.Parameters.GetValueOrDefault("exitBar", "15"));
        if (context.BarIndex == enter)
        {
            var kind = context.Vector.Direction == Direction.Long
                ? OrderIntentKind.OpenLong
                : OrderIntentKind.OpenShort;
            context.Sink.OnIntent(new OrderIntent(kind, 0, "fixed-window"), context.Bar);
        }
        else if (context.BarIndex == exit)
        {
            context.Sink.OnIntent(new OrderIntent(OrderIntentKind.ClosePosition, 0, "fixed-window"), context.Bar);
        }
    }
}
