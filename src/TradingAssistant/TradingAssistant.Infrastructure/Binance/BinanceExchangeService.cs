using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using TradingAssistant.Application;

namespace TradingAssistant.Infrastructure.Binance;

public class BinanceExchangeService : IExchangeService
{
    private readonly TradingAssistant.BinanceService _inner;

    public BinanceExchangeService(TradingAssistant.BinanceService inner)
    {
        _inner = inner;
    }

    public Task<decimal> GetLastPriceAsync(string symbol, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<bool> PlaceMarketOrderAsync(string symbol, PositionSide positionSide, OrderSide side, decimal quantity, CancellationToken cancellationToken)
        => _inner.TryClosePositionAtMarketAsync(symbol, side == OrderSide.Buy ? -Math.Abs(quantity) : Math.Abs(quantity), cancellationToken);

    public Task CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        => _inner.CancelAllOrdersAsync(symbol, cancellationToken);

    public bool TryGetLeverage(string symbol, out int leverage)
        => _inner.TryGetLeverage(symbol, out leverage);

    public Task<bool> TryPlaceStopLossAsync(string symbol, decimal entryPrice, decimal positionQuantity, decimal roi, bool includeFees, CancellationToken cancellationToken = default)
        => _inner.TryPlaceStopLossAsync(symbol, entryPrice, positionQuantity, roi, includeFees, cancellationToken);

    public Task<bool> TryCancelStopLossAsync(string symbol, CancellationToken cancellationToken = default)
        => _inner.TryCancelStopLossAsync(symbol, cancellationToken);

    public Task<bool> TryPlaceTakeProfitAsync(string symbol, decimal entryPrice, decimal positionQuantity, decimal roi, bool includeFees, CancellationToken cancellationToken = default)
        => _inner.TryPlaceTakeProfitAsync(symbol, entryPrice, positionQuantity, roi, includeFees, cancellationToken);

    public Task<bool> TryCancelTakeProfitAsync(string symbol, CancellationToken cancellationToken = default)
        => _inner.TryCancelTakeProfitAsync(symbol, cancellationToken);

    public Task<bool> TryPlaceTakeProfitBehindAsync(string symbol, decimal price, decimal quantity, OrderSide orderSide, CancellationToken cancellationToken = default)
        => _inner.TryPlaceTakeProfitBehindAsync(symbol, price, quantity, orderSide, cancellationToken);

    public Task<bool> TryCancelSteppedTrailingAsync(string symbol, CancellationToken cancellationToken = default)
        => _inner.TryCancelSteppedTrailingAsync(symbol, cancellationToken);

    public Task<bool> TryPlaceTrailingStopAsync(string symbol, OrderSide orderSide, decimal quantity, decimal callbackRate, decimal? price = null, CancellationToken cancellationToken = default)
        => _inner.TryPlaceTrailingStopAsync(symbol, orderSide, quantity, callbackRate, price, cancellationToken);

    public Task<bool> TryCancelTrailingStopAsync(string symbol, CancellationToken cancellationToken = default)
        => _inner.TryCancelTrailingStopAsync(symbol, cancellationToken);

    public Task<bool> TrySubscribeToPriceAsync(string symbol, Action<IBinanceKline> action, CancellationToken cancellationToken = default)
        => _inner.TrySubscribeToPriceAsync(symbol, action, cancellationToken);

    public Task<bool> TryUnsubscribeFromPriceAsync(string symbol)
        => _inner.TryUnsubscribeFromPriceAsync(symbol);

    public void SubscribeToAccountUpdates(Action<DataEvent<BinanceFuturesStreamAccountUpdate>> action)
        => _inner.SubscribeToAccountUpdates(action);
}


