﻿using System.Collections.Concurrent;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Converters.SystemTextJson;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

namespace TradingAssistant
{
    public class BinanceService
    {
        private const string EntryOrderIdFormat = "{0}-entry-order";
        private const string StopLossIdFormat = "{0}-stop-loss";
        private const string BreakEvenIdFormat = "{0}-break-even";
        private const string TakeProfitIdFormat = "{0}-take-profit";
        private const string SteppedTrailingIdFormat = "{0}-stepped-trailing";
        private const string TrailingStopIdFormat = "{0}-trailing-stop";
        private const int MaxCandlesPerRequest = 1500;
        private readonly ILogger<BinanceService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBinanceRestClient _rest;
        private readonly IBinanceSocketClient _socket;
        private readonly List<Action<DataEvent<BinanceFuturesStreamConfigUpdate>>> _leverageUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamMarginUpdate>>> _marginUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamAccountUpdate>>> _accountUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceFuturesStreamOrderUpdate>>> _orderUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceStreamEvent>>> _listenKeyExpiredSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceStrategyUpdate>>> _strategyUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceGridUpdate>>> _gridUpdateSubscriptions = [];
        private readonly List<Action<DataEvent<BinanceConditionOrderTriggerRejectUpdate>>> _conditionalOrderTriggerRejectUpdateSubscriptions = [];
        private readonly CandleClosedProvider _candleClosedProvider = new();
        private readonly ConcurrentDictionary<string, BinanceFuturesUsdtSymbol> _symbols = [];
        private readonly ConcurrentDictionary<string, int> _leverages = [];
        private readonly ConcurrentDictionary<string, UpdateSubscription> _priceSubscriptions = [];
        private readonly ConcurrentDictionary<string, CircularTimeSeries<string, IBinanceKline>> _candlesticks = [];
        private KlineInterval _interval;
        private int _candlestickSize;
        private string? _listenKey;

        public BinanceService(ILogger<BinanceService> logger,
            IConfiguration configuration,
            IBinanceRestClient rest,
            IBinanceSocketClient socket)
        {
            _logger = logger;
            _configuration = configuration;
            _rest = rest;
            _socket = socket;

            ConfigureService();
        }

        private async void ConfigureService()
        {
            _interval = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _candlestickSize = _configuration.GetValue<int>("Binance:Service:CandlestickSize");

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

            await SubscribeToCandlestickUpdatesAsync();

            _logger.LogInformation("Binance Service configured");
        }

        private static decimal ApplyMarketQuantityFilter(decimal quantity,
            decimal price,
            BinanceSymbolMinNotionalFilter? minNotionalFilter,
            BinanceSymbolMarketLotSizeFilter? marketLotSizeFilter)
        {
            if (minNotionalFilter is not null)
            {
                var notionalValue = quantity * price;

                if (notionalValue < minNotionalFilter.MinNotional)
                {
                    quantity = minNotionalFilter.MinNotional / price;
                }
            }

            if (marketLotSizeFilter is not null)
            {
                quantity = Math.Max(Math.Min(quantity, marketLotSizeFilter.MaxQuantity), marketLotSizeFilter.MinQuantity);

                var remainder = (quantity - marketLotSizeFilter.MinQuantity) % marketLotSizeFilter.StepSize;

                if (remainder > 0)
                {
                    quantity -= remainder;
                    quantity += marketLotSizeFilter.StepSize;
                }
            }

            return quantity;
        }

        private static decimal ApplyLimitQuantityFilter(decimal quantity,
            decimal price,
            BinanceSymbolMinNotionalFilter? minNotionalFilter,
            BinanceSymbolLotSizeFilter? lotSizeFilter)
        {
            if (minNotionalFilter is not null)
            {
                var notionalValue = quantity * price;

                if (notionalValue < minNotionalFilter.MinNotional)
                {
                    quantity = minNotionalFilter.MinNotional / price;
                }
            }

            if (lotSizeFilter is not null)
            {
                quantity = Math.Max(Math.Min(quantity, lotSizeFilter.MaxQuantity), lotSizeFilter.MinQuantity);

                var remainder = (quantity - lotSizeFilter.MinQuantity) % lotSizeFilter.StepSize;

                if (remainder > 0)
                {
                    quantity -= remainder;
                    quantity += lotSizeFilter.StepSize;
                }
            }

            return quantity;
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

        private static void EnsureStopLossRoiIsValid(decimal roi)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roi);
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
            var exchangeData = _rest.UsdFuturesApi.ExchangeData;
            var getExchangeInfoResult = await exchangeData.GetExchangeInfoAsync(cancellationToken);

            if (!getExchangeInfoResult.GetResultOrError(out var exchangeInfo, out var getExchangeInfoError))
            {
                _logger.LogError("Get exchange info failed. {Error}", getExchangeInfoError);

                return false;
            }

            var symbols = exchangeInfo.Symbols.Where(symbol => symbol.BaseAsset != "USDC")
                .Where(symbol => symbol.QuoteAsset == "USDT")
                .Where(symbol => symbol.ContractType == ContractType.Perpetual);

            foreach (var symbol in symbols)
            {
                if (!_symbols.TryAdd(symbol.Name, symbol))
                {
                    _logger.LogWarning("Store {Symbol} info failed. {Error}", symbol.Name, getExchangeInfoError);
                }
            }

            _logger.LogInformation("Get exchange info symbols succeeded");

            return true;
        }

        private async Task<bool> TryConfigureMarginTypeAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;

            await Parallel.ForEachAsync(_symbols, cancellationToken, async (symbol, token) =>
            {
                await account.ChangeMarginTypeAsync(symbol.Key, FuturesMarginType.Cross, ct: token);
            });

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

            await Parallel.ForEachAsync(_leverages, cancellationToken, async (leverage, token) =>
            {
                await account.ChangeInitialLeverageAsync(leverage.Key, leverage.Value, ct: token);
            });

            _logger.LogInformation("Leverage configuration finished");

            return true;
        }

        private void HandleLeverageUpdate(DataEvent<BinanceFuturesStreamConfigUpdate> configUpdate)
        {
            var symbol = configUpdate.Data.LeverageUpdateData!.Symbol;
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

        public IObservable<CircularTimeSeries<string, IBinanceKline>> GetCandleClosedEvent()
        {
            return _candleClosedProvider;
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

        public async Task<BinanceFuturesAccountInfo?> TryGetAccountInformationAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getAccountInfoResult = await account.GetAccountInfoAsync(ct: cancellationToken);

            if (!getAccountInfoResult.GetResultOrError(out var accountInfo, out var getAccountInfoError))
            {
                _logger.LogError("Get account information failed. {Error}", getAccountInfoError);

                return default;
            }

            return accountInfo;
        }

        public async Task<BinancePositionDetailsUsdt?> TryGetPositionInformationAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getPositionResult = await account.GetPositionInformationAsync(symbol, ct: cancellationToken);

            if (!getPositionResult.GetResultOrError(out var positions, out var getPositionError))
            {
                _logger.LogError("Get position information failed. {Error}", getPositionError);

                return default;
            }

            return positions.FirstOrDefault(p => p.EntryPrice != 0 && p.Quantity != 0);
        }

        public async Task<IEnumerable<BinancePositionDetailsUsdt>> TryGetPositionsAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getPositionsResult = await account.GetPositionInformationAsync(ct: cancellationToken);

            if (!getPositionsResult.GetResultOrError(out var positions, out var getPositionsError))
            {
                _logger.LogError("Get positions failed. {Error}", getPositionsError);

                return [];
            }

            return positions.Where(p => p.EntryPrice != 0 && p.Quantity != 0);
        }

        public async Task<IEnumerable<BinanceFuturesOrder>> TryGetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var getOpenOrdersResult = await trading.GetOpenOrdersAsync(symbol, ct: cancellationToken);

            if (!getOpenOrdersResult.GetResultOrError(out var openOrders, out var getOpenOrdersError))
            {
                _logger.LogError("Get open orders failed. {Error}", getOpenOrdersError);

                return [];
            }

            return openOrders;
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

        private async Task SubscribeToCandlestickUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var subscribeToKlineUpdatesResult = await _socket.UsdFuturesApi.SubscribeToKlineUpdatesAsync(_symbols.Keys,
                _interval,
                @event =>
                {
                    var candle = @event.Data.Data;
                    var symbol = @event.Data.Symbol;
                    var isCandleClosed = candle.Final;

                    if (isCandleClosed)
                    {
                        var candlestick = _candlesticks.GetOrAdd(symbol, value: new CircularTimeSeries<string, IBinanceKline>(symbol, _candlestickSize));

                        candlestick.Add(candle.OpenTime, candle);
                        _candleClosedProvider.Update(candlestick);
                    }
                },
                cancellationToken);

            if (!subscribeToKlineUpdatesResult.Success)
            {
                _logger.LogWarning("Subscribe to all candlesticks failed. {Error}", subscribeToKlineUpdatesResult.Error);

                return;
            }

            _logger.LogInformation("Subscribe to all candlesticks updates succeeded");

            var candlesPerRequest = _candlestickSize < MaxCandlesPerRequest
                ? _candlestickSize
                : MaxCandlesPerRequest;

            var weight = candlesPerRequest switch
            {
                >= 1 and < 100 => 1,
                >= 100 and < 500 => 2,
                >= 500 and <= 1000 => 5,
                _ => 10
            };

            await Parallel.ForEachAsync(_symbols, cancellationToken, async (symbol, token) =>
            {
                var candles = new List<IBinanceKline>();
                var endTime = default(DateTime?);
                var requiredRequests = (int)Math.Ceiling(_candlestickSize / (double)MaxCandlesPerRequest);

                for (var requestCount = 0; requestCount < requiredRequests; requestCount++)
                {
                    var exchangeData = _rest.UsdFuturesApi.ExchangeData;
                    var getKlinesResult = await exchangeData.GetKlinesAsync(symbol.Key,
                        _interval,
                        endTime: endTime,
                        limit: candlesPerRequest,
                        ct: token);

                    if (!getKlinesResult.GetResultOrError(out var klines, out var getKlinesError))
                    {
                        _logger.LogWarning("Get {Symbol} candlestick failed. {Error}", symbol.Key, getKlinesError);

                        return;
                    }

                    candles.AddRange(klines);

                    endTime = klines.FirstOrDefault()?.OpenTime;

                    if (klines.Count() < MaxCandlesPerRequest)
                    {
                        break;
                    }
                }

                var candlestick = _candlesticks.GetOrAdd(symbol.Key,
                    value: new CircularTimeSeries<string, IBinanceKline>(symbol.Key, _candlestickSize));

                foreach (var candle in candles)
                {
                    candlestick.Add(candle.OpenTime, candle);
                }

                _logger.LogInformation("Get {Symbol} {Count} {Interval} candles succeeded",
                    symbol.Key,
                    candles.Count,
                    EnumConverter.GetString(_interval));
            });

            _logger.LogInformation("Get candlesticks succeeded");
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
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelOrderResult = await trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(StopLossIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old SL order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryCancelBreakEvenAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelOrderResult = await trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(BreakEvenIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old BE order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryCancelTakeProfitAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelOrderResult = await trading.CancelOrderAsync(symbol,
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
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelOrderResult = await trading.CancelOrderAsync(symbol,
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
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelOrderResult = await trading.CancelOrderAsync(symbol,
                origClientOrderId: string.Format(TrailingStopIdFormat, symbol.ToLower()),
                ct: cancellationToken);

            if (!cancelOrderResult.Success)
            {
                _logger.LogDebug("Cancel old Trailing Stop order failed. {Error}", cancelOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceEntryOrderAsync(string symbol,
            OrderSide orderSide,
            FuturesOrderType orderType,
            decimal quantity,
            decimal? entryPrice = default, CancellationToken cancellationToken = default)
        {
            if (orderType != FuturesOrderType.Market && orderType != FuturesOrderType.Limit)
            {
                return false;
            }

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var isLimitOrder = orderType is FuturesOrderType.Limit;

            quantity = isLimitOrder
                ? ApplyLimitQuantityFilter(quantity, entryPrice!.Value, symbolInformation?.MinNotionalFilter, symbolInformation?.LotSizeFilter)
                : ApplyMarketQuantityFilter(quantity, entryPrice!.Value, symbolInformation?.MinNotionalFilter, symbolInformation?.MarketLotSizeFilter);

            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                orderSide,
                orderType,
                quantity,
                price: isLimitOrder ? ApplyPriceFilter(entryPrice.Value, symbolInformation?.PriceFilter) : null,
                timeInForce: isLimitOrder ? TimeInForce.GoodTillCanceled : null,
                newClientOrderId: isLimitOrder ? string.Format(EntryOrderIdFormat, symbol.ToLower()) : null,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place Entry order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceStopLossAsync(string symbol,
            decimal entryPrice,
            decimal quantity,
            decimal roi,
            CancellationToken cancellationToken = default)
        {
            EnsureStopLossRoiIsValid(roi);

            var trading = _rest.UsdFuturesApi.Trading;

            if (!TryGetLeverage(symbol, out var leverage))
            {
                return false;
            }

            var stopLossPrice = StopLossPrice.Calculate(entryPrice, quantity, roi, leverage, includeFees: true);

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                quantity.AsOrderSide().Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(stopLossPrice, symbolInformation?.PriceFilter),
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

        public async Task<bool> TryPlaceBreakEvenAsync(string symbol,
            OrderSide positionSide,
            decimal stopPrice,
            CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                positionSide.Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(stopPrice, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(BreakEvenIdFormat, symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place BE order failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }

        public async Task<bool> TryPlaceTakeProfitBehindAsync(string symbol,
            decimal price,
            decimal quantity,
            OrderSide orderSide,
            CancellationToken cancellationToken = default)
        {
            var takeProfitPrice = TakeProfitPrice.Calculate(price, quantity, offset: 0, includeFees: true);

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                orderSide,
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(takeProfitPrice, symbolInformation?.PriceFilter),
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

            if (!TryGetLeverage(symbol, out var leverage))
            {
                return false;
            }

            var takeProfitPrice = TakeProfitPrice.Calculate(entryPrice, quantity, roi, leverage, includeFees: true);

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                quantity.AsOrderSide().Reverse(),
                FuturesOrderType.TakeProfitMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(takeProfitPrice, symbolInformation?.PriceFilter),
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

            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
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

        public async Task<bool> TryClosePositionAtMarketAsync(string symbol,
            decimal quantity,
            CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                quantity.AsOrderSide().Reverse(),
                FuturesOrderType.Market,
                Math.Abs(quantity),
                reduceOnly: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Close position at market price failed. {Error}", placeOrderResult.Error);

                return false;
            }

            return true;
        }
    }
}
