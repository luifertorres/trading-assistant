using Binance.Net.Enums;

namespace TradingAssistant.Application;

public interface IExchangeService
{
    Task<decimal> GetLastPriceAsync(string symbol, CancellationToken cancellationToken);
    Task<bool> PlaceMarketOrderAsync(string symbol, PositionSide positionSide, OrderSide side, decimal quantity, CancellationToken cancellationToken);
}


