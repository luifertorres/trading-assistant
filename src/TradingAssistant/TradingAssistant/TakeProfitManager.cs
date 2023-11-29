using System.Collections.Concurrent;

using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class TakeProfitManager : BackgroundService
    {
        private const string TakeProfitIdFormat = "{0}-take-profit";
        private readonly ILogger<TakeProfitManager> _logger;
        private readonly ConcurrentDictionary<string, UpdateSubscription> _markPriceSubscriptions = new();
        private string? _listenKey;
        private int _takeProfitCount;

        public TakeProfitManager(ILogger<TakeProfitManager> logger)
        {
            _logger = logger;

            const string Key = "Lvf6bhdcC4xkdruLKv6VTyJ8ETJrNcnemmpCo7VpPwIQUu2WJolCHpiDdj9bYZ3B";
            const string Secret = "agiBFSg0TpCuGz17lcOLT4H4GOJ3k8cA3lW7Oi2LiRE7VGtFAuF9uFAHimqixFjt";

            BinanceRestClient.SetDefaultOptions(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
            BinanceSocketClient.SetDefaultOptions(o => o.ApiCredentials = new ApiCredentials(Key, Secret));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var binanceRestClient = new BinanceRestClient();
            using var binanceSocketClient = new BinanceSocketClient();

            var account = binanceRestClient.UsdFuturesApi.Account;
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

            var usdFuturesApi = binanceSocketClient.UsdFuturesApi;
            var updateSubscription = await usdFuturesApi.SubscribeToUserDataUpdatesAsync(_listenKey,
                onLeverageUpdate: _ => { },
                onMarginUpdate: _ => { },
                onAccountUpdate: @event => HandleAccountUpdate(@event, binanceRestClient, binanceSocketClient, stoppingToken),
                onOrderUpdate: @event => HandleOrderUpdate(@event, binanceRestClient, stoppingToken),
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

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> accountUpdateEvent,
            BinanceRestClient restClient,
            BinanceSocketClient socketClient,
            CancellationToken cancellationToken = default)
        {
            foreach (var position in accountUpdateEvent.Data.UpdateData.Positions)
            {
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    var account = restClient.UsdFuturesApi.Account;
                    var getPositionInformationResult = await account.GetPositionInformationAsync(position.Symbol, ct: cancellationToken);

                    if (!getPositionInformationResult.GetResultOrError(out var positions, out var getPositionInformationError))
                    {
                        _logger.LogError("Get position information has failed. {Error}", getPositionInformationError);

                        continue;
                    }

                    var leverage = positions.First(p => p.Symbol == position.Symbol).Leverage;
                    var subscribeToMarkPriceResult = await socketClient.UsdFuturesApi.SubscribeToMarkPriceUpdatesAsync(position.Symbol,
                        updateInterval: default,
                        onMessage: markPriceEvent => UpdateTakeProfit(markPriceEvent, position, leverage, restClient, cancellationToken),
                        cancellationToken);

                    if (!subscribeToMarkPriceResult.GetResultOrError(out var newMarkPriceSubscription, out var subscribeToMarkPriceError))
                    {
                        _logger.LogError("Subscribe to Mark Price has failed. {Error}", subscribeToMarkPriceError);

                        continue;
                    }

                    if (_markPriceSubscriptions.TryRemove(position.Symbol, out var oldMarkPriceSubscription))
                    {
                        await oldMarkPriceSubscription.CloseAsync();
                    }

                    if (!_markPriceSubscriptions.TryAdd(position.Symbol, newMarkPriceSubscription))
                    {
                        _logger.LogError("Storing Mark Price subscription for {Symbol} has failed", position.Symbol);
                    }
                }
                else
                {
                    if (_markPriceSubscriptions.TryRemove(position.Symbol, out var oldMarkPriceSubscription))
                    {
                        await oldMarkPriceSubscription.CloseAsync();
                    }
                }
            }
        }

        private async void UpdateTakeProfit(DataEvent<BinanceFuturesUsdtStreamMarkPrice> markPriceEvent,
            BinanceFuturesStreamPosition position,
            int leverage,
            BinanceRestClient restClient,
            CancellationToken cancellationToken = default)
        {
            var markPrice = markPriceEvent.Data.MarkPrice;
            var sign = position.Quantity > 0 ? 1 : -1;
            var pnlPercentage = sign * ((markPrice / position.EntryPrice) - 1) * 100;
            var stepPercentage = 100m / leverage;

            if (pnlPercentage < stepPercentage)
            {
                return;
            }

            var percentageRemainder = pnlPercentage % stepPercentage;
            var multiplier = (pnlPercentage - percentageRemainder) / stepPercentage;
            var targetPercentage = stepPercentage * (multiplier - 1);
            var newTrailingStopPrice = position.EntryPrice * (1 + (sign * targetPercentage / 100));
            var trading = restClient.UsdFuturesApi.Trading;
            var getOpenOrderResult = await trading.GetOpenOrderAsync(position.Symbol,
                origClientOrderId: string.Format(TakeProfitIdFormat, position.Symbol.ToLower()),
                ct: cancellationToken);

            if (getOpenOrderResult.GetResultOrError(out var oldTrailingStop, out var getOpenOrderError))
            {
                if (newTrailingStopPrice != oldTrailingStop.StopPrice)
                {
                    var cancelOrderResult = await trading.CancelOrderAsync(position.Symbol,
                        origClientOrderId: string.Format(oldTrailingStop.ClientOrderId, position.Symbol.ToLower()),
                        ct: cancellationToken);

                    if (!cancelOrderResult.Success)
                    {
                        _logger.LogDebug("Cancel old TP order has failed. {Error}", cancelOrderResult.Error);

                        return;
                    }
                }
            }
            else
            {
                _logger.LogDebug("Get old TP order has failed. {Error}", getOpenOrderError);
            }

            var symbolInformation = await GetSymbolInformation(position.Symbol, restClient, cancellationToken);
            var placeOrderResult = await trading.PlaceOrderAsync(position.Symbol,
                position.Quantity.AsOrderSide().Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(newTrailingStopPrice, symbolInformation?.PriceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: string.Format(TakeProfitIdFormat, position.Symbol.ToLower()),
                priceProtect: true,
                ct: cancellationToken);

            if (!placeOrderResult.Success)
            {
                _logger.LogError("Place new TP order has failed. {Error}", placeOrderResult.Error);
            }
        }

        private async void HandleOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> @event,
            BinanceRestClient binanceRestClient,
            CancellationToken stoppingToken)
        {
            var orderUpdate = @event.Data.UpdateData;

            switch (orderUpdate.ExecutionType)
            {
                case ExecutionType.New:
                    break;
                case ExecutionType.Canceled:
                    break;
                case ExecutionType.Replaced:
                    break;
                case ExecutionType.Rejected:
                    break;
                case ExecutionType.Trade:
                    break;
                case ExecutionType.Expired:
                    break;
                case ExecutionType.Amendment:
                    break;
                case ExecutionType.TradePrevention:
                    break;
                default:
                    break;
            }
        }

        private async Task PlaceStopLossOrder(BinanceFuturesStreamOrderUpdateData orderUpdate,
            BinanceRestClient client,
            CancellationToken stoppingToken)
        {
            var position = await GetPositionDetailsAsync(orderUpdate, client, stoppingToken);
            var trading = client.UsdFuturesApi.Trading;

            if (position is null)
            {
                _takeProfitCount = 0;

                await trading.CancelAllOrdersAsync(orderUpdate.Symbol, ct: stoppingToken);

                return;
            }

            var oldStopLossOrder = await GetStopLossOrderAsync(position.Symbol, client, stoppingToken);
            var symbolInformation = await GetSymbolInformation(position.Symbol, client, stoppingToken);
            var isNewStopLossOrder = await TryPlaceNewStopLossOrderAsync(client, position, symbolInformation?.PriceFilter, stoppingToken);

            if (isNewStopLossOrder && oldStopLossOrder is not null)
            {
                await trading.CancelOrderAsync(position.Symbol, origClientOrderId: oldStopLossOrder.ClientOrderId, ct: stoppingToken);
            }
        }

        private async Task<BinancePositionDetailsUsdt?> GetPositionDetailsAsync(BinanceFuturesStreamOrderUpdateData orderUpdate,
            BinanceRestClient client,
            CancellationToken stoppingToken)
        {
            var account = client.UsdFuturesApi.Account;
            var positionInformationResult = await account.GetPositionInformationAsync(orderUpdate.Symbol, ct: stoppingToken);

            if (!positionInformationResult.Success)
            {
                _logger.LogError("Get position information has failed. {Error}", positionInformationResult.Error);

                return null;
            }

            var position = positionInformationResult.Data.FirstOrDefault(p => p.Symbol == orderUpdate.Symbol);

            if (position is null)
            {
                return null;
            }

            if (position.Quantity == 0)
            {
                return null;
            }

            return position;
        }

        private async Task<BinanceFuturesOrder?> GetStopLossOrderAsync(string symbol,
            BinanceRestClient client,
            CancellationToken stoppingToken)
        {
            var trading = client.UsdFuturesApi.Trading;
            var openOrdersResult = await trading.GetOpenOrdersAsync(symbol, ct: stoppingToken);

            if (!openOrdersResult.Success)
            {
                _logger.LogError("Get open orders has failed. {Error}", openOrdersResult.Error);

                return null;
            }

            return openOrdersResult.Data.Where(o => o.Symbol == symbol)
                .FirstOrDefault(o => o.Type == FuturesOrderType.StopMarket);
        }

        private async Task<BinanceFuturesUsdtSymbol?> GetSymbolInformation(string symbol,
            BinanceRestClient client,
            CancellationToken stoppingToken)
        {
            var exchangeInfoResult = await client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(stoppingToken);

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

        private async Task<bool> TryPlaceNewStopLossOrderAsync(BinanceRestClient client,
            BinancePositionDetailsUsdt position,
            BinanceSymbolPriceFilter? priceFilter,
            CancellationToken stoppingToken)
        {
            var trading = client.UsdFuturesApi.Trading;
            var positionSideAsOrderSide = position.Quantity.AsOrderSide();
            var stopLossMoneyQuantity = 0.95m;
            var sign = positionSideAsOrderSide == OrderSide.Buy ? 1 : -1;
            var positionCost = position.EntryPrice * Math.Abs(position.Quantity);
            var price = (positionCost - (sign * stopLossMoneyQuantity)) / Math.Abs(position.Quantity);
            var placeStopLossOrderResult = await trading.PlaceOrderAsync(position.Symbol,
                positionSideAsOrderSide.Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(price, priceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: $"{position.Symbol.ToLower()}-stop-loss-{++_takeProfitCount}",
                priceProtect: true,
                ct: stoppingToken);

            if (!placeStopLossOrderResult.Success)
            {
                _logger.LogError("Place stop loss has failed. {Error}", placeStopLossOrderResult.Error);

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
