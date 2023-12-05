using System.Collections.Concurrent;

using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class TakeProfitManager(ILogger<TakeProfitManager> logger) : BackgroundService
    {
        private const string Key = "Lvf6bhdcC4xkdruLKv6VTyJ8ETJrNcnemmpCo7VpPwIQUu2WJolCHpiDdj9bYZ3B";
        private const string Secret = "agiBFSg0TpCuGz17lcOLT4H4GOJ3k8cA3lW7Oi2LiRE7VGtFAuF9uFAHimqixFjt";
        private const string TakeProfitIdFormat = "{0}-take-profit";
        private readonly ILogger<TakeProfitManager> _logger = logger;
        private readonly BinanceRestClient _rest = new(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
        private readonly BinanceSocketClient _socket = new(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
        private readonly ConcurrentDictionary<string, UpdateSubscription> _priceSubscriptions = new();
        private readonly ConcurrentDictionary<string, int> _leverages = new();
        private readonly object _synchronizer = new();
        private Task _updateTakeProfitTask = Task.CompletedTask;
        private string? _listenKey;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getLeverageBracketsResult = await account.GetBracketsAsync(ct: stoppingToken);

            if (!getLeverageBracketsResult.GetResultOrError(out var brackets, out var getLeverageBracketsError))
            {
                _logger.LogError("Get leverage brackets failed. {Error}", getLeverageBracketsError);

                return;
            }

            brackets.ToList().ForEach(bracket =>
            {
                _leverages.TryAdd(bracket.Symbol, bracket.Brackets.Max(b => b.InitialLeverage));
            });

            _leverages.ToList().ForEach(async (leverage) =>
            {
                await account.ChangeMarginTypeAsync(symbol: leverage.Key, marginType: FuturesMarginType.Cross, ct: stoppingToken);
                await account.ChangeInitialLeverageAsync(symbol: leverage.Key, leverage: leverage.Value, ct: stoppingToken);
            });

            var startUserStreamResult = await account.StartUserStreamAsync(stoppingToken);

            if (!startUserStreamResult.GetResultOrError(out _listenKey, out var startUserStreamError))
            {
                _logger.LogError("Start user stream has failed. {Error}", startUserStreamError);

                return;
            }

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

                    try
                    {
                        await account.KeepAliveUserStreamAsync(_listenKey, stoppingToken);
                    }
                    catch
                    {
                    }
                }
            }, stoppingToken);

            var usdFuturesApi = _socket.UsdFuturesApi;
            var updateSubscription = await usdFuturesApi.SubscribeToUserDataUpdatesAsync(_listenKey,
                onLeverageUpdate: HandleLeverageUpdate,
                onMarginUpdate: _ => { },
                onAccountUpdate: @event => HandleAccountUpdate(@event, stoppingToken),
                onOrderUpdate: _ => { },
                onListenKeyExpired: _ => { },
                onStrategyUpdate: _ => { },
                onGridUpdate: _ => { },
                onConditionalOrderTriggerRejectUpdate: _ => { },
                ct: stoppingToken);

            if (!updateSubscription.Success)
            {
                _logger.LogError("Subscribe to user data updates has failed. {Error}", startUserStreamResult.Error);

                return;
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void HandleLeverageUpdate(DataEvent<BinanceFuturesStreamConfigUpdate> configUpdate)
        {
            var symbol = configUpdate.Data.LeverageUpdateData.Symbol;
            var leverage = configUpdate.Data.LeverageUpdateData.Leverage;

            _leverages.AddOrUpdate(symbol!, leverage, (_, _) => leverage);
        }

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> accountUpdateEvent, CancellationToken cancellationToken = default)
        {
            foreach (var position in accountUpdateEvent.Data.UpdateData.Positions)
            {
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    if (!_leverages.TryGetValue(position.Symbol, out var leverage))
                    {
                        _logger.LogError("Get {Symbol} leverage failed", position.Symbol);

                        continue;
                    }

                    var trailingStop = new TrailingStop(position.EntryPrice, position.Quantity, leverage);
                    var subscribeToPriceResult = await _socket.UsdFuturesApi.SubscribeToKlineUpdatesAsync(position.Symbol,
                        interval: KlineInterval.OneMinute,
                        onMessage: @event => UpdateTakeProfit(@event.Data.Data.ClosePrice, position, trailingStop, cancellationToken),
                        cancellationToken);

                    if (!subscribeToPriceResult.GetResultOrError(out var newPriceSubscription, out var subscribeToPriceError))
                    {
                        _logger.LogError("Subscribe to {Symbol} price failed. {Error}", position.Symbol, subscribeToPriceError);

                        continue;
                    }

                    if (_priceSubscriptions.TryRemove(position.Symbol, out var oldPriceSubscription))
                    {
                        await oldPriceSubscription.CloseAsync();
                    }

                    if (!_priceSubscriptions.TryAdd(position.Symbol, newPriceSubscription))
                    {
                        _logger.LogError("Store {Symbol} price subscription failed", position.Symbol);
                    }
                }
                else
                {
                    if (_priceSubscriptions.TryRemove(position.Symbol, out var oldPriceSubscription))
                    {
                        await oldPriceSubscription.CloseAsync();
                    }
                }
            }
        }

        private void UpdateTakeProfit(decimal currentPrice, BinanceFuturesStreamPosition position, TrailingStop trailingStop, CancellationToken cancellationToken = default)
        {
            lock (_synchronizer)
            {
                if (!_updateTakeProfitTask.IsCompleted)
                {
                    return;
                }

                _updateTakeProfitTask = Task.Run(() => UpdateTrailingStop(currentPrice, position, trailingStop, cancellationToken),
                    cancellationToken);
            }
        }

        private async Task UpdateTrailingStop(decimal currentPrice, BinanceFuturesStreamPosition position, TrailingStop trailingStop, CancellationToken cancellationToken)
        {
            if (!trailingStop.TryAdvance(currentPrice, out var stopPrice))
            {
                return;
            }

            await TryCancelTakeProfitAsync(position.Symbol, cancellationToken);
            await TryPlaceTakeProfitAsync(position.Symbol,
                stopPrice!.Value,
                position.Quantity.AsOrderSide().Reverse(),
                cancellationToken);
        }

        private async Task<BinancePositionDetailsUsdt?> GetPositionDetailsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var positionInformationResult = await account.GetPositionInformationAsync(symbol, ct: cancellationToken);

            if (!positionInformationResult.Success)
            {
                _logger.LogError("Get position information has failed. {Error}", positionInformationResult.Error);

                return null;
            }

            var position = positionInformationResult.Data.FirstOrDefault(p => p.Symbol == symbol);

            if (position?.Quantity == 0)
            {
                return null;
            }

            return position;
        }

        private async Task<BinanceFuturesUsdtSymbol?> GetSymbolInformation(string symbol, CancellationToken cancellationToken = default)
        {
            var exchangeInfoResult = await _rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken);

            if (!exchangeInfoResult.Success)
            {
                _logger.LogError("Get exchange info has failed. {Error}", exchangeInfoResult.Error);

                return null;
            }

            var symbolInformation = exchangeInfoResult.Data.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInformation is null)
            {
                _logger.LogError("{Symbol} symbol doesn't have exchange info", symbol);

                return null;
            }

            return symbolInformation;
        }

        private async Task<bool> TryCancelTakeProfitAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cancelOrderResult = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(TakeProfitIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old TP order has failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        private async Task<bool> TryPlaceTakeProfitAsync(string symbol,
            decimal price,
            OrderSide orderSide,
            CancellationToken cancellationToken = default)
        {
            var symbolInformation = await GetSymbolInformation(symbol, cancellationToken);
            var placeOrderResult = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                orderSide,
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(price, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(TakeProfitIdFormat, symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place new TP order has failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        private static decimal ApplyPriceFilter(decimal price, BinanceSymbolPriceFilter? filter)
        {
            if (filter is null)
            {
                return price;
            }

            price = Math.Max(Math.Min(price, filter.MaxPrice), filter.MinPrice);

            var remainder = (price - filter.MinPrice) % filter.TickSize;

            return price - remainder;
        }
    }
}
