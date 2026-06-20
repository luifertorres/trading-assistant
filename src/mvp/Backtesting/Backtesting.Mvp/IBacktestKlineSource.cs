using Binance.Net.Interfaces;

namespace Backtesting.Mvp;

public interface IBacktestKlineSource
{
    IReadOnlyList<IBinanceKline> GetKlines();
}
