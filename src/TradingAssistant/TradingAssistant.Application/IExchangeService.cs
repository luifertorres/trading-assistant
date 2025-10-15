using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;

namespace TradingAssistant.Application;

public interface IExchangeService
{
    Task<decimal> GetLastPriceAsync(string symbol, CancellationToken cancellationToken);
    Task<bool> PlaceMarketOrderAsync(string symbol, PositionSide positionSide, OrderSide side, decimal quantity, CancellationToken cancellationToken);

    // Operations used by managers
    Task CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default);
    bool TryGetLeverage(string symbol, out int leverage);

    Task<bool> TryPlaceStopLossAsync(string symbol, decimal entryPrice, decimal positionQuantity, decimal roi, bool includeFees, CancellationToken cancellationToken = default);
    Task<bool> TryCancelStopLossAsync(string symbol, CancellationToken cancellationToken = default);

    Task<bool> TryPlaceTakeProfitAsync(string symbol, decimal entryPrice, decimal positionQuantity, decimal roi, bool includeFees, CancellationToken cancellationToken = default);
    Task<bool> TryCancelTakeProfitAsync(string symbol, CancellationToken cancellationToken = default);
    Task<bool> TryPlaceTakeProfitBehindAsync(string symbol, decimal price, decimal quantity, OrderSide orderSide, CancellationToken cancellationToken = default);
    Task<bool> TryCancelSteppedTrailingAsync(string symbol, CancellationToken cancellationToken = default);

    Task<bool> TryPlaceTrailingStopAsync(string symbol, OrderSide orderSide, decimal quantity, decimal callbackRate, decimal? price = null, CancellationToken cancellationToken = default);
    Task<bool> TryCancelTrailingStopAsync(string symbol, CancellationToken cancellationToken = default);

    Task<bool> TrySubscribeToPriceAsync(string symbol, Action<IBinanceKline> action, CancellationToken cancellationToken = default);
    Task<bool> TryUnsubscribeFromPriceAsync(string symbol);
    void SubscribeToAccountUpdates(Action<DataEvent<BinanceFuturesStreamAccountUpdate>> action);
}


