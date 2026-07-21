using System.Collections.Concurrent;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Converters.SystemTextJson;
using CryptoExchange.Net.Objects.Sockets;
using FASTER.core;
using MediatR;

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
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly IBinanceRestClient _rest;
        private readonly IBinanceSocketClient _socket;
        private readonly IPublisher _publisher;
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
        private KlineInterval _interval;
        private int _candlestickSize;
        private string? _listenKey;

        public BinanceService(ILogger<BinanceService> logger,
            IConfiguration configuration,
            FasterKV<CandleId, Candle> cache,
            IBinanceRestClient rest,
            IBinanceSocketClient socket,
            IPublisher publisher)
        {
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _rest = rest;
            _socket = socket;
            _publisher = publisher;

            ConfigureServiceAsync().GetAwaiter().GetResult();
        }

        private async Task ConfigureServiceAsync()
        {
            _interval = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _candlestickSize = _configuration.GetValue<int>("Binance:Service:CandlestickSize");

            if (!await TryConfigureSymbolsAsync())
            {
                throw new BinanceServiceNotConfiguredException();
            }

            if (!await TryConfigureMarginTypeAsync())
            {
                throw new BinanceServiceNotConfiguredException();
            }

            if (!await TryConfigureLeverageAsync())
            {
                throw new BinanceServiceNotConfiguredException();
            }

            if (!await TryStartUserDataStreamAsync())
            {
                throw new BinanceServiceNotConfiguredException();
            }

            await SubscribeToCandlestickUpdatesAsync();

            _logger.LogInformation("Binance Service configured");
        }

        public bool TryReduceMarketQuantity(decimal quantity,
            decimal price,
            BinanceSymbolMinNotionalFilter? minNotionalFilter,
            BinanceSymbolMarketLotSizeFilter? marketLotSizeFilter,
            out decimal reducedQuantity)
        {
            if (minNotionalFilter is null || marketLotSizeFilter is null)
            {
                reducedQuantity = quantity;

                return false;
            }

            var expectedQuantity = quantity - marketLotSizeFilter.StepSize;

            reducedQuantity = ApplyMarketQuantityFilter(expectedQuantity, price, minNotionalFilter, marketLotSizeFilter);

            if (reducedQuantity > expectedQuantity)
            {
                return false;
            }

            return true;
        }

        public decimal ApplyMarketQuantityFilter(decimal quantity,
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

            if (!startUserStreamResult.Success || startUserStreamResult.Data is null)
            {
                _logger.LogError("Start user stream has failed. {Error}", startUserStreamResult.Error);

                return false;
            }

            _listenKey = startUserStreamResult.Data;

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
                        // The exception can be ignored because we just need
                        // to keep alive the stream whenever possible.
                    }
                }
            }, cancellationToken);

            var usdFuturesApi = _socket.UsdFuturesApi.Account;
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

            if (!getExchangeInfoResult.Success || getExchangeInfoResult.Data is null)
            {
                _logger.LogError("Get exchange info failed. {Error}", getExchangeInfoResult.Error);

                return false;
            }

            var exchangeInfo = getExchangeInfoResult.Data;

            var getLast24hTickersResult = await exchangeData.GetTickersAsync(cancellationToken);

            if (!getLast24hTickersResult.Success || getLast24hTickersResult.Data is null)
            {
                _logger.LogError("Get last 24-hour tickers failed. {Error}", getLast24hTickersResult.Error);

                return false;
            }

            var last24hTickers = getLast24hTickersResult.Data;

            var highestTradedVolumeSymbols = last24hTickers.Where(ticker => ticker.QuoteVolume > 000_000_000)
                .Select(ticker => ticker.Symbol);

            var symbols = exchangeInfo.Symbols.Where(symbol => symbol.Status is SymbolStatus.Trading)
                .Where(symbol => symbol.BaseAsset is not "USDC")
                .Where(symbol => symbol.QuoteAsset is "USDT" or "USDC")
                .Where(symbol => symbol.ContractType == ContractType.Perpetual)
                .IntersectBy(highestTradedVolumeSymbols, symbol => symbol.Name);

            foreach (var symbol in symbols)
            {
                if (!_symbols.TryAdd(symbol.Name, symbol))
                {
                    _logger.LogWarning("Store {Symbol} info failed. {Error}", symbol.Name, getExchangeInfoResult.Error);
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
                var changeMarginTypeResult = await account.ChangeMarginTypeAsync(symbol.Key, FuturesMarginType.Cross, ct: token);
                if (!changeMarginTypeResult.Success
                    && changeMarginTypeResult.Error?.Code != -4046)
                {
                    _logger.LogWarning(
                        "Change margin type for {Symbol} failed. {Error}",
                        symbol.Key,
                        changeMarginTypeResult.Error);
                }
            });

            _logger.LogInformation("Margin type configuration finished");

            return true;
        }

        private async Task<bool> TryConfigureLeverageAsync(CancellationToken cancellationToken = default)
        {
            SubscribeToLeverageUpdates(HandleLeverageUpdate);

            var account = _rest.UsdFuturesApi.Account;
            var getLeverageBracketsResult = await account.GetBracketsAsync(ct: cancellationToken);

            if (!getLeverageBracketsResult.Success || getLeverageBracketsResult.Data is null)
            {
                _logger.LogError("Get leverage brackets failed. {Error}", getLeverageBracketsResult.Error);

                return false;
            }

            var brackets = getLeverageBracketsResult.Data;

            brackets.ToList().ForEach(bracket =>
            {
                _leverages.TryAdd(bracket.Symbol, bracket.Brackets.Max(b => b.InitialLeverage));
            });

            await Parallel.ForEachAsync(_symbols, cancellationToken, async (symbol, token) =>
            {
                if (_leverages.TryGetValue(symbol.Key, out var leverage))
                {
                    await account.ChangeInitialLeverageAsync(symbol.Key, leverage, ct: token);
                }
            });

            _logger.LogInformation("Leverage configuration finished");

            return true;
        }

        private void HandleLeverageUpdate(DataEvent<BinanceFuturesStreamConfigUpdate> configUpdate)
        {
            var data = configUpdate.Data;

            if (data.ConfigUpdateData is not { MultiAssetMode: true })
            {
                var symbol = data.LeverageUpdateData!.Symbol;
                var leverage = data.LeverageUpdateData.Leverage;

                _leverages.AddOrUpdate(symbol!, leverage, (_, _) => leverage);
            }
        }

        public bool TryGetSymbolInformation(string symbol, out BinanceFuturesUsdtSymbol? symbolInformation)
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

        public async Task<BinanceFuturesAccountInfo?> TryGetAccountInformationAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getAccountInfoResult = await account.GetAccountInfoV2Async(ct: cancellationToken);

            if (!getAccountInfoResult.Success || getAccountInfoResult.Data is null)
            {
                _logger.LogError("Get account information failed. {Error}", getAccountInfoResult.Error);

                return default;
            }

            return getAccountInfoResult.Data;
        }

        public async Task<BinancePositionDetailsUsdt?> TryGetPositionInformationAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getPositionResult = await account.GetPositionInformationAsync(symbol, ct: cancellationToken);

            if (!getPositionResult.Success || getPositionResult.Data is null)
            {
                _logger.LogError("Get position information failed. {Error}", getPositionResult.Error);

                return default;
            }

            return getPositionResult.Data.FirstOrDefault(p => p.EntryPrice != 0 && p.Quantity != 0);
        }

        public async Task<IEnumerable<BinancePositionDetailsUsdt>> TryGetPositionsAsync(CancellationToken cancellationToken = default)
        {
            var account = _rest.UsdFuturesApi.Account;
            var getPositionsResult = await account.GetPositionInformationAsync(ct: cancellationToken);

            if (!getPositionsResult.Success || getPositionsResult.Data is null)
            {
                _logger.LogError("Get positions failed. {Error}", getPositionsResult.Error);

                return [];
            }

            return getPositionsResult.Data.Where(p => p.EntryPrice != 0 && p.Quantity != 0);
        }

        public async Task<IEnumerable<BinanceUsdFuturesOrder>> TryGetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var getOpenOrdersResult = await trading.GetOpenOrdersAsync(symbol, ct: cancellationToken);

            if (!getOpenOrdersResult.Success || getOpenOrdersResult.Data is null)
            {
                _logger.LogError("Get open orders failed. {Error}", getOpenOrdersResult.Error);

                return [];
            }

            return getOpenOrdersResult.Data;
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
            Action<IBinanceKline> action,
            CancellationToken cancellationToken = default)
        {
            var subscribeToPriceResult = await _socket.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbol,
                interval: KlineInterval.OneMinute,
                onMessage: @event => action(@event.Data.Data),
                ct: cancellationToken);

            if (!subscribeToPriceResult.Success || subscribeToPriceResult.Data is null)
            {
                _logger.LogError("Subscribe to {Symbol} price failed. {Error}", symbol, subscribeToPriceResult.Error);

                return false;
            }

            var newPriceSubscription = subscribeToPriceResult.Data;

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
            var candlesPerRequest = _candlestickSize < MaxCandlesPerRequest
                ? _candlestickSize
                : MaxCandlesPerRequest;

            var timeFrames = new[] { _interval };

            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            foreach (var timeFrame in timeFrames)
            {
                var symbolGroups = _symbols.Keys.Chunk(size: Environment.ProcessorCount);

                foreach (var symbols in symbolGroups)
                {
                    var subscribeToKlineUpdatesResult = await _socket.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbols,
                        timeFrame,
                        @event =>
                        {
                            var kline = @event.Data.Data;

                            if (kline.Final && (kline.Interval == _interval))
                            {
                                var symbol = @event.Data.Symbol;

                                using var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>();

                                var candleId = new CandleId(symbol, kline.Interval, kline.OpenTime);
                                var candle = new Candle
                                {
                                    Symbol = symbol,
                                    Interval = kline.Interval,
                                    OpenTime = kline.OpenTime,
                                    CloseTime = kline.CloseTime,
                                    OpenPrice = kline.OpenPrice,
                                    HighPrice = kline.HighPrice,
                                    LowPrice = kline.LowPrice,
                                    ClosePrice = kline.ClosePrice,
                                };

                                session.Upsert(ref candleId, ref candle);

                                _publisher.Publish(new CandleClosedNotification(candleId));
                            }
                        },
                        ct: cancellationToken);

                    if (!subscribeToKlineUpdatesResult.Success)
                    {
                        _logger.LogWarning("Subscribe to {TimeFrame} candlesticks failed. {Error}",
                            EnumConverter.GetString(timeFrame),
                            subscribeToKlineUpdatesResult.Error);

                        return;
                    }
                }

                _logger.LogInformation("Subscribe to {TimeFrame} candlesticks updates succeeded",
                    EnumConverter.GetString(timeFrame));

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                foreach (var symbols in symbolGroups)
                {
                    await Parallel.ForEachAsync(symbols,
                        parallelOptions,
                        async (symbol, token) =>
                        {
                    var totalKlines = new List<IBinanceKline>();
                    var utcDateTime = GetCurrentUtcTime();
                    var endTime = (DateTime?)utcDateTime.AddSeconds(-(int)_interval);
                    var requiredRequests = (int)Math.Ceiling(_candlestickSize / (double)MaxCandlesPerRequest);

                    for (var requestCount = 0; requestCount < requiredRequests; requestCount++)
                    {
                        var exchangeData = _rest.UsdFuturesApi.ExchangeData;
                                var getKlinesResult = await exchangeData.GetKlinesAsync(symbol,
                            timeFrame,
                            endTime: endTime,
                            limit: candlesPerRequest,
                            ct: token);

                        if (!getKlinesResult.Success || getKlinesResult.Data is null)
                        {
                            _logger.LogWarning("Get {Symbol} {TimeFrame} candlestick failed. {Error}",
                                        symbol,
                                EnumConverter.GetString(timeFrame),
                                getKlinesResult.Error);

                            return;
                        }

                        var klines = getKlinesResult.Data;

                        totalKlines.AddRange(klines);

                                endTime = klines.FirstOrDefault()?.OpenTime.AddSeconds(-(int)_interval);

                        if (klines.Count() < MaxCandlesPerRequest)
                        {
                            break;
                        }
                    }

                            //if (totalKlines.Count >= _candlestickSize)
                    {
                                using var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>();

                        foreach (var kline in totalKlines)
                        {
                                    var candleId = new CandleId(symbol, timeFrame, kline.OpenTime);
                            var candle = new Candle
                            {
                                        Symbol = symbol,
                                Interval = timeFrame,
                                OpenTime = kline.OpenTime,
                                CloseTime = kline.CloseTime,
                                OpenPrice = kline.OpenPrice,
                                HighPrice = kline.HighPrice,
                                LowPrice = kline.LowPrice,
                                ClosePrice = kline.ClosePrice,
                            };

                            session.Upsert(ref candleId, ref candle);
                        }
                    }

                    _logger.LogInformation("Get {Count} {Symbol} {Interval} candles succeeded",
                        Math.Min(totalKlines.Count, _candlestickSize),
                                symbol,
                        EnumConverter.GetString(timeFrame));
                });
            }
            }

            _logger.LogInformation("Get candlesticks succeeded");
        }

        public void TriggerLastCandleClosedNotifications()
        {
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            using var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>();

            foreach (var symbol in _symbols.Keys)
            {
                var utcDateTime = GetCurrentUtcTime();
                var lastOpenCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(utcDateTime) / (long)_interval;
                var lastOpenCandleOpenTime = DateTimeConverter.ConvertFromSeconds((double)lastOpenCandleOpenTimeTotalSeconds * (long)_interval);
                var lastClosedCandleOpenTime = lastOpenCandleOpenTime.AddSeconds(-(int)_interval);
                var candleId = new CandleId(symbol, _interval, lastClosedCandleOpenTime);

                _publisher.Publish(new CandleClosedNotification(candleId));
            }
        }

        private static DateTime GetCurrentUtcTime()
        {
            //var localTime = new TimeOnly(13, 22, 01);
            //var localDate = new DateOnly(2024, 12, 24);
            //var localDateTime = new DateTime(localDate, localTime, DateTimeKind.Local);
            //var utcDateTime = localDateTime.ToUniversalTime();
            var utcDateTime = DateTime.UtcNow;

            return utcDateTime;
        }

        public async Task CancelAllOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var cancelAllOrdersResult = await trading.CancelAllOrdersAsync(symbol, ct: cancellationToken);

            if (!cancelAllOrdersResult.Success)
            {
                _logger.LogDebug("Cancel all orders failed. {Error}", cancelAllOrdersResult.Error);
            }
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
            decimal? entryPrice = default,
            CancellationToken cancellationToken = default)
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
            decimal positionQuantity,
            decimal roi,
            bool includeFees = false,
            CancellationToken cancellationToken = default)
        {
            EnsureStopLossRoiIsValid(roi);

            var trading = _rest.UsdFuturesApi.Trading;

            if (!TryGetLeverage(symbol, out var leverage))
            {
                return false;
            }

            var stopLossPrice = StopLossPrice.Calculate(entryPrice, positionQuantity, roi, leverage, includeFees);

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                positionQuantity.AsOrderSide().Reverse(),
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
            decimal positionQuantity,
            decimal roi,
            bool includeFees = false,
            CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;

            if (!TryGetLeverage(symbol, out var leverage))
            {
                return false;
            }

            var takeProfitPrice = TakeProfitPrice.Calculate(entryPrice, positionQuantity, roi, leverage, includeFees);

            TryGetSymbolInformation(symbol, out var symbolInformation);

            var orderType = FuturesOrderType.TakeProfitMarket;

            var maybeQuantity = orderType switch
            {
                FuturesOrderType.TakeProfitMarket => (decimal?)null,
                FuturesOrderType.Limit => Math.Abs(positionQuantity),

                _ => throw new NotSupportedException($"Order type {orderType} is not supported"),
            };

            var maybeStopPrice = orderType switch
            {
                FuturesOrderType.TakeProfitMarket => ApplyPriceFilter(takeProfitPrice, symbolInformation?.PriceFilter),
                FuturesOrderType.Limit => (decimal?)null,

                _ => throw new NotSupportedException($"Order type {orderType} is not supported"),
            };

            var maybePrice = orderType switch
            {
                FuturesOrderType.TakeProfitMarket => (decimal?)null,
                FuturesOrderType.Limit => ApplyPriceFilter(takeProfitPrice, symbolInformation?.PriceFilter),

                _ => throw new NotSupportedException($"Order type {orderType} is not supported"),
            };

            var maybeClosePosition = orderType switch
            {
                FuturesOrderType.TakeProfitMarket => true,
                FuturesOrderType.Limit => (bool?)null,

                _ => throw new NotSupportedException($"Order type {orderType} is not supported"),
            };

            var maybePriceProtect = orderType switch
            {
                FuturesOrderType.TakeProfitMarket => true,
                FuturesOrderType.Limit => (bool?)null,

                _ => throw new NotSupportedException($"Order type {orderType} is not supported"),
            };

            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                positionQuantity.AsOrderSide().Reverse(),
                orderType,
                quantity: maybeClosePosition is true ? null : maybeQuantity,
                price: maybePrice,
                stopPrice: maybeStopPrice,
                closePosition: maybeClosePosition,
                timeInForce: TimeInForce.GoodTillCanceled,
                reduceOnly: maybeClosePosition is true ? null : true,
                newClientOrderId: string.Format(TakeProfitIdFormat, symbol.ToLower()),
                priceProtect: maybePriceProtect,
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

            callbackRate = callbackRate switch
            {
                < 0.1m => 0.1m,
                > 10m => 10m,
                _ => callbackRate,
            };

            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                orderSide,
                FuturesOrderType.TrailingStopMarket,
                quantity: Math.Abs(quantity),
                timeInForce: TimeInForce.GoodTillCanceled,
                reduceOnly: true,
                newClientOrderId: string.Format(TrailingStopIdFormat, symbol.ToLower()),
                activationPrice: price.HasValue ? ApplyPriceFilter(price.Value, symbolInformation?.PriceFilter) : null,
                callbackRate: Math.Round(callbackRate, decimals: 2),
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
            decimal positionQuantity,
            CancellationToken cancellationToken = default)
        {
            var trading = _rest.UsdFuturesApi.Trading;
            var placeOrderResult = await trading.PlaceOrderAsync(symbol,
                positionQuantity.AsOrderSide().Reverse(),
                FuturesOrderType.Market,
                Math.Abs(positionQuantity),
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
