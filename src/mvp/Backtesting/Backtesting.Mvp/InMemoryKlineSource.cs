using Binance.Net.Interfaces;

namespace Backtesting.Mvp;

public sealed class InMemoryKlineSource(IReadOnlyList<IBinanceKline> klines) : IBacktestKlineSource
{
    public IReadOnlyList<IBinanceKline> GetKlines() => klines;
}
