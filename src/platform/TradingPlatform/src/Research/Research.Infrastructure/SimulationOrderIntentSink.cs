using Research.Application;
using Research.Domain;
using TradingPlatform.Kernel;

namespace Research.Infrastructure;

/// <summary>Turns <see cref="OrderIntent"/> into positions, fees, trades, and an equity series (simulation).</summary>
public sealed class SimulationOrderIntentSink : ISimulationOrderIntentSink
{
    private readonly SimulationConfiguration _cfg;
    private readonly List<TradeRecord> _trades = [];
    private readonly List<EquityPoint> _equity = [];
    private decimal _cash;
    private decimal? _entryPrice;
    private decimal _quantity;
    private DateTimeOffset? _entryTime;
    private decimal _entryFees;
    private readonly PositionSide _side;

    public SimulationOrderIntentSink(SimulationConfiguration cfg, PositionSide side)
    {
        _cfg = cfg;
        _cash = cfg.InitialCapital;
        _side = side;
    }

    public IReadOnlyList<TradeRecord> Trades => _trades;
    public IReadOnlyList<EquityPoint> Equity => _equity;

    public void OnIntent(in OrderIntent intent, in OhlcBar signalBar)
    {
        var price = intent.ExitPrice ?? signalBar.Close;
        switch (intent.Kind)
        {
            case OrderIntentKind.OpenLong when _side == PositionSide.Long && _entryPrice is null:
            {
                var notional = _cfg.InitialCapital * _cfg.PositionNotionalFraction;
                var qty = notional / price;
                if (qty <= 0)
                    return;
                var fee = qty * price * (_cfg.FeeBpsPerSide / 10_000m);
                _cash -= fee;
                _entryFees = fee;
                _quantity = qty;
                _entryPrice = price;
                _entryTime = signalBar.CloseTime;
                break;
            }
            case OrderIntentKind.OpenShort when _side == PositionSide.Short && _entryPrice is null:
            {
                var notional = _cfg.InitialCapital * _cfg.PositionNotionalFraction;
                var qty = notional / price;
                if (qty <= 0)
                    return;
                var fee = qty * price * (_cfg.FeeBpsPerSide / 10_000m);
                _cash -= fee;
                _entryFees = fee;
                _quantity = qty;
                _entryPrice = price;
                _entryTime = signalBar.CloseTime;
                break;
            }
            case OrderIntentKind.ClosePosition when _entryPrice is { } ep && _entryTime is { } et:
            {
                var qty = _quantity;
                var exitFee = qty * price * (_cfg.FeeBpsPerSide / 10_000m);
                decimal gross = _side switch
                {
                    PositionSide.Long => qty * (price - ep),
                    PositionSide.Short => qty * (ep - price),
                    _ => 0
                };
                _cash += gross - exitFee;
                var totalFees = _entryFees + exitFee;
                var net = gross - totalFees;
                _trades.Add(new TradeRecord(et, signalBar.CloseTime, ep, price, qty, gross, totalFees, net));
                _entryPrice = null;
                _entryTime = null;
                _quantity = 0;
                _entryFees = 0;
                break;
            }
        }
    }

    public void OnBarClosed(in OhlcBar bar)
    {
        _equity.Add(new EquityPoint(bar.CloseTime, MarkEquity(bar.Close)));
    }

    private decimal MarkEquity(decimal markPrice)
    {
        if (_entryPrice is null || _quantity == 0)
            return _cash;
        return _side switch
        {
            PositionSide.Long => _cash + _quantity * (markPrice - _entryPrice.Value),
            PositionSide.Short => _cash + _quantity * (_entryPrice.Value - markPrice),
            _ => _cash
        };
    }

    public static decimal MaxDrawdownFraction(IReadOnlyList<EquityPoint> series)
    {
        if (series.Count == 0)
            return 0;
        decimal peak = series[0].Equity;
        decimal maxDd = 0;
        foreach (var p in series)
        {
            if (p.Equity > peak)
                peak = p.Equity;
            if (peak > 0)
            {
                var dd = (peak - p.Equity) / peak;
                if (dd > maxDd)
                    maxDd = dd;
            }
        }

        return maxDd;
    }
}
