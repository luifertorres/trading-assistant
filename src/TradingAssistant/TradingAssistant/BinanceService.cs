using System.Collections.Concurrent;

using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class BinanceService
    {
        private const string Key = "Lvf6bhdcC4xkdruLKv6VTyJ8ETJrNcnemmpCo7VpPwIQUu2WJolCHpiDdj9bYZ3B";
        private const string Secret = "agiBFSg0TpCuGz17lcOLT4H4GOJ3k8cA3lW7Oi2LiRE7VGtFAuF9uFAHimqixFjt";
        private const string StopLossIdFormat = "{0}-stop-loss";
        private const string TakeProfitIdFormat = "{0}-take-profit";
        private const string SteppedTrailingIdFormat = "{0}-stepped-trailing";
        private const string TrailingStopIdFormat = "{0}-trailing-stop";
        private readonly ILogger<BinanceService> _logger;
        private readonly BinanceRestClient _rest = new(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
        private readonly BinanceSocketClient _socket = new(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
        private readonly List<Action<DataEvent<BinanceFuturesStreamConfigUpdate>>> _leverageUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamMarginUpdate>>> _marginUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamAccountUpdate>>> _accountUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamOrderUpdate>>> _orderUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceStreamEvent>>> _listenKeyExpiredSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceStrategyUpdate>>> _strategyUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceGridUpdate>>> _gridUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceConditionOrderTriggerRejectUpdate>>> _conditionalOrderTriggerRejectUpdateSubscriptions = [];
        private readonly ConcurrentDictionary<string, BinanceFuturesUsdtSymbol> _symbols = [];
        private readonly ConcurrentDictionary<string, int> _leverages = [];
        private readonly ConcurrentDictionary<string, UpdateSubscription> _priceSubscriptions = [];
        private string? _listenKey;

        public BinanceService(ILogger<BinanceService> logger)
        {
            _logger = logger;

            Initialize();
        }

        private async void Initialize()
        {
            if (!await TryConfigureSymbolsAsync())
            {
                throw new Exception();
            }

            if (!await TryConfigureMarginTypeAsync())
            {
                throw new Exception();
            }

            if (!await TryConfigureLeverageAsync())
            {
                throw new Exception();
            }

            if (!await TryStartUserDataStreamAsync())
            {
                throw new Exception();
            }

            _logger.LogInformation("Binance Service configured");
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

        private void EnsureLossValuesAreValid(decimal? moneyToLose, decimal? roi)
        {
            if (moneyToLose is null && roi is null)
            {
                _logger.LogCritical("Both money to lose or ROI are null. " +
                    "You have to pass one of them to place a Stop Loss");

                throw new ArgumentNullException($"{moneyToLose} or {roi}",
                    "Both money to lose or ROI are null. " +
                    "You have to pass one of them to place a Stop Loss");
            }

            if (moneyToLose is not null && roi is not null)
            {
                _logger.LogCritical("Both money to lose or ROI are passed. " +
                    "You have to pass only one of them to place a Stop Loss");

                throw new ArgumentNullException($"{moneyToLose} or {roi}",
                    "Both money to lose or ROI are passed. " +
                    "You have to pass only one of them to place a Stop Loss");
            }

            if (moneyToLose is 0 || roi is 0)
            {
                _logger.LogCritical("Either money to lose or ROI are 0. " +
                    "You have to pass one of them as non-zero value to place a Stop Loss");

                throw new ArgumentNullException($"{moneyToLose} or {roi}",
                    "Either money to lose or ROI are 0. " +
                    "You have to pass one of them as non-zero value to place a Stop Loss");
            }
        }

        private async Task<bool> TryStartUserDataStreamAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var startUserStreamResult = await account.StartUserStreamAsync(cancellationToken);

            if (!startUserStreamResult.GetResultOrError(out _listenKey, out var startUserStreamError))
            {
                _logger.LogError("Start user stream has failed. {Error}", startUserStreamError);

                return false;
            }

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);

                    try
                    {
                        await account.KeepAliveUserStreamAsync(_listenKey, cancellationToken);
                    }
                    catch
                    {
                    }
                }
            }, cancellationToken);

            var usdFuturesApi = _socket.UsdFuturesApi;
            var updateSubscription = await usdFuturesApi.SubscribeToUserDataUpdatesAsync(_listenKey,
                onLeverageUpdate: @event => _leverageUpdateSubscriptions.ForEach(s => s(@event)),
                onMarginUpdate: @event => _marginUpdateSubscriptions.ForEach(s => s(@event)),
                onAccountUpdate: @event => _accountUpdateSubscriptions.ForEach(s => s(@event)),
                onOrderUpdate: @event => _orderUpdateSubscriptions.ForEach(s => s(@event)),
                onListenKeyExpired: @event => _listenKeyExpiredSubscriptions.ForEach(s => s(@event)),
                onStrategyUpdate: @event => _strategyUpdateSubscriptions.ForEach(s => s(@event)),
                onGridUpdate: @event => _gridUpdateSubscriptions.ForEach(s => s(@event)),
                onConditionalOrderTriggerRejectUpdate: @event => _conditionalOrderTriggerRejectUpdateSubscriptions.ForEach(s => s(@event)),
                ct: cancellationToken);

            if (!updateSubscription.Success)
            {
                _logger.LogError("Subscribe to user data updates failed. {Error}", startUserStreamResult.Error);

                return false;
            }

            _logger.LogInformation("Subscribe to user data updates succeeded");

            return true;
        }

        private async Task<bool> TryConfigureSymbolsAsync(CancellationToken cancellationToken = default)
        {
            var getExchangeInfoResult = await _rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken);

            if (!getExchangeInfoResult.GetResultOrError(out var exchangeInfo, out var getExchangeInfoError))
            {
                _logger.LogError("Get exchange info failed. {Error}", getExchangeInfoError);

                return false;
            }

            exchangeInfo.Symbols.ToList().ForEach(symbol => _symbols.TryAdd(symbol.Name, symbol));
            _logger.LogInformation("Get exchange info symbols succeeded");

            return true;
        }

        private async Task<bool> TryConfigureMarginTypeAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;

            foreach (var symbol in _symbols)
            {
                await account.ChangeMarginTypeAsync(symbol.Key, FuturesMarginType.Cross, ct: cancellationToken);
            }

            _logger.LogInformation("Margin type configuration finished");

            return true;
        }

        private async Task<bool> TryConfigureLeverageAsync(CancellationToken cancellationToken = default)
        {
            SubscribeToLeverageUpdates(HandleLeverageUpdate);

            var account = _rest.UsdFuturesApi.Account;
            var getLeverageBracketsResult = await account.GetBracketsAsync(ct: cancellationToken);

            if (!getLeverageBracketsResult.GetResultOrError(out var brackets, out var getLeverageBracketsError))
            {
                _logger.LogError("Get leverage brackets failed. {Error}", getLeverageBracketsError);

                return false;
            }

            brackets.ToList().ForEach(bracket =>
            {
                _leverages.TryAdd(bracket.Symbol, bracket.Brackets.Max(b => b.InitialLeverage));
            });

            foreach (var leverage in _leverages)
            {
                await account.ChangeInitialLeverageAsync(leverage.Key, leverage.Value, ct: cancellationToken);
            }

            _logger.LogInformation("Leverage configuration finished");

            return true;
        }

        private void HandleLeverageUpdate(DataEvent<BinanceFuturesStreamConfigUpdate> configUpdate)
        {
            var symbol = configUpdate.Data.LeverageUpdateData.Symbol;
            var leverage = configUpdate.Data.LeverageUpdateData.Leverage;

            _leverages.AddOrUpdate(symbol!, leverage, (_, _) => leverage);
        }

        private bool TryGetSymbolInformation(string symbol, out BinanceFuturesUsdtSymbol? symbolInformation)
        {
            if (!_symbols.TryGetValue(symbol, out symbolInformation))
            {
                _logger.LogError("Get {Symbol} symbol information failed", symbol);

                return false;
            }

            return true;
        }

        public void SubscribeToLeverageUpdates(Action<DataEvent<BinanceFuturesStreamConfigUpdate>> action)
        {
            _leverageUpdateSubscriptions.Add(action);
        }

        public void SubscribeToMarginUpdates(Action<DataEvent<BinanceFuturesStreamMarginUpdate>> action)
        {
            _marginUpdateSubscriptions.Add(action);
        }

        public void SubscribeToAccountUpdates(Action<DataEvent<BinanceFuturesStreamAccountUpdate>> action)
        {
            _accountUpdateSubscriptions.Add(action);
        }

        public void SubscribeToOrderUpdates(Action<DataEvent<BinanceFuturesStreamOrderUpdate>> action)
        {
            _orderUpdateSubscriptions.Add(action);
        }

        public void SubscribeToListenKeyExpired(Action<DataEvent<BinanceStreamEvent>> action)
        {
            _listenKeyExpiredSubscriptions.Add(action);
        }

        public void SubscribeToStrategyUpdates(Action<DataEvent<BinanceStrategyUpdate>> action)
        {
            _strategyUpdateSubscriptions.Add(action);
        }

        public void SubscribeToGridUpdates(Action<DataEvent<BinanceGridUpdate>> action)
        {
            _gridUpdateSubscriptions.Add(action);
        }

        public void SubscribeToConditionalOrderTriggerRejectUpdates(Action<DataEvent<BinanceConditionOrderTriggerRejectUpdate>> action)
        {
            _conditionalOrderTriggerRejectUpdateSubscriptions.Add(action);
        }

        public bool TryGetLeverage(string symbol, out int leverage)
        {
            if (!_leverages.TryGetValue(symbol, out leverage))
            {
                _logger.LogError("Get {Symbol} leverage failed", symbol);

                return false;
            }

            return true;
        }

        public async Task<bool> TryUnsubscribeFromPriceAsync(string symbol)
        {
            if (!_priceSubscriptions.TryRemove(symbol, out var oldPriceSubscription))
            {
                return false;
            }

            await oldPriceSubscription.CloseAsync();

            return true;
        }

        public async Task<bool> TrySubscribeToPriceAsync(string symbol,
            Action<decimal> action,
            CancellationToken cancellationToken = default)
        {
            var subscribeToPriceResult = await _socket.UsdFuturesApi.SubscribeToKlineUpdatesAsync(symbol,
            interval: KlineInterval.OneMinute,
            onMessage: @event => action(@event.Data.Data.ClosePrice),
            cancellationToken);

            if (!subscribeToPriceResult.GetResultOrError(out var newPriceSubscription, out var subscribeToPriceError))
            {
                _logger.LogError("Subscribe to {Symbol} price failed. {Error}", symbol, subscribeToPriceError);

                return false;
            }

            if (_priceSubscriptions.TryRemove(symbol, out var oldPriceSubscription))
            {
                await oldPriceSubscription.CloseAsync();
            }

            if (!_priceSubscriptions.TryAdd(symbol, newPriceSubscription))
            {
                _logger.LogDebug("Store {Symbol} price subscription failed", symbol);
                await newPriceSubscription.CloseAsync();

                return false;
            }

            return true;
        }

        public async Task CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelAllOrdersResult = await trading.CancelAllOrdersAsync(symbol, ct: cancellationToken);

            if (!cancelAllOrdersResult.Success)
            {
                _logger.LogDebug("Cancel all orders failed. {Error}", cancelAllOrdersResult.Error);
            }

            return;
        }

        public async Task<bool> TryCancelStopLossAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cancelOrderResult = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(StopLossIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old SL order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryCancelTakeProfitAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cancelOrderResult = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(TakeProfitIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old TP order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryCancelSteppedTrailingAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cancelOrderResult = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(SteppedTrailingIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old Stepped Trailing order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryCancelTrailingStopAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cancelOrderResult = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(TrailingStopIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old Trailing Stop order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceStopLossAsync(string symbol,
            decimal entryPrice,
            decimal quantity,
            decimal? moneyToLose = default,
            decimal? roi = default,
            CancellationToken cancellationToken = default)
        {
            EnsureLossValuesAreValid(moneyToLose, roi);

            var trading = _rest.UsdFuturesApi.Trading;
            var sign = decimal.Sign(quantity);
            var positionCost = entryPrice * Math.Abs(quantity);

            if (moneyToLose is null)
            {
                if (!TryGetLeverage(symbol, out var leverage))
                {
                    return false;
                }

                var margin = positionCost / leverage;

                moneyToLose = margin * roi / 100;
            }

            var stopLossPrice = (positionCost - (sign * moneyToLose!.Value)) / Math.Abs(quantity);
            var entryCommissionCost = positionCost * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var stopLossPriceAfterEntryCommission = stopLossPrice + entryCommissionPriceDistance;
            var lossAfterFees = 1 - sign * 0.05m / 100;
            var stopLossPriceAfterTotalFees = stopLossPriceAfterEntryCommission / lossAfterFees;

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                quantity.AsOrderSide().Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(stopLossPriceAfterTotalFees, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(StopLossIdFormat, symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place SL order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceTakeProfitBehindAsync(string symbol,
            decimal price,
            OrderSide orderSide,
            CancellationToken cancellationToken = default)
        {
            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                orderSide,
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(price, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(SteppedTrailingIdFormat, symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place stepped TP order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceTakeProfitAsync(string symbol,
            decimal entryPrice,
            decimal quantity,
            decimal roi,
            CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var sign = decimal.Sign(quantity);
            var positionCost = entryPrice * Math.Abs(quantity);

            if (!TryGetLeverage(symbol, out var leverage))
            {
                return false;
            }

            var margin = positionCost / leverage;
            var profit = margin * roi / 100;
            var takeProfitPrice = (positionCost + (sign * profit)) / Math.Abs(quantity);
            var entryCommissionCost = positionCost * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var takeProfitPriceAfterEntryCommission = takeProfitPrice + entryCommissionPriceDistance;
            var profitAfterExitCommission = 1 - (sign * 0.05m / 100);
            var takeProfitPriceAfterTotalFees = takeProfitPriceAfterEntryCommission / profitAfterExitCommission;

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                quantity.AsOrderSide().Reverse(),
                FuturesOrderType.TakeProfitMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(takeProfitPriceAfterTotalFees, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(TakeProfitIdFormat, symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place TP order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceTrailingStopAsync(string symbol,
            OrderSide orderSide,
            decimal quantity,
            decimal callbackRate,
            decimal? price = null,
            CancellationToken cancellationToken = default)
        {
            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                orderSide,
                FuturesOrderType.TrailingStopMarket,
                quantity: Math.Abs(quantity),
                timeInForce: TimeInForce.GoodTillCanceled,
                reduceOnly: true,
                newClientOrderId: string.Format(TrailingStopIdFormat, symbol.ToLower()),
                activationPrice: price.HasValue ? ApplyPriceFilter(price.Value, symbolInformation?.PriceFilter) : null,
                callbackRate: Math.Round(callbackRate, decimals: 1),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place trailing TP order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }
    }
}
