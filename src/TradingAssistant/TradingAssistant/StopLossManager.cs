using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class StopLossManager : BackgroundService
    {
        private readonly ILogger<StopLossManager> _logger;
        private string? _listenKey;
        private int _stopLossCount;

        public StopLossManager(ILogger<StopLossManager> logger)
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
                onAccountUpdate: HandleAccountUpdate,
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

        private void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event)
        {
            //var position = @event.Data.UpdateData.Positions.FirstOrDefault(p => p.Symbol == Symbol);

            //if (position is null)
            //{
            //    return;
            //}

            //if (position.Quantity == 0)
            //{
            //    Task.Run(async () =>
            //    {
            //        var cancelOrdersResult = await _client.UsdFuturesApi.Trading.CancelAllOrdersAsync(Symbol, ct: stoppingToken);
            //    }, stoppingToken);

            //    return;
            //}

            //if (_bestBlocks is null)
            //{
            //    return;
            //}

            //Task.Run(async () =>
            //{
            //    var cancelOrdersResult = await _client.UsdFuturesApi.Trading.CancelAllOrdersAsync(Symbol, ct: stoppingToken);
            //    var side = position.Quantity.ToOrderSide();
            //    var placeStopLossOrderResult = await _client.UsdFuturesApi.Trading.PlaceOrderAsync(Symbol,
            //        side.Reverse(),
            //        FuturesOrderType.StopMarket,
            //        null,
            //        stopPrice: side == OrderSide.Buy ? _bestBlocks.Bids.ElementAt(1).Key : _bestBlocks.Asks.ElementAt(1).Key,
            //        closePosition: true,
            //        newClientOrderId: $"{Symbol.ToLower()}-stop-loss",
            //        ct: stoppingToken);

            //    if (!placeStopLossOrderResult.Success)
            //    {
            //        _logger.LogError("StopLoss result error: {Error}", placeStopLossOrderResult.Error);
            //    }

            //    var entryPrice = (side == OrderSide.Buy ? _bestBlocks.Bids.ElementAt(0).Key : _bestBlocks.Asks.ElementAt(0).Key);
            //    var takeProfitPrice = side == OrderSide.Buy ? _bestBlocks.Asks.ElementAt(0).Key : _bestBlocks.Bids.ElementAt(0).Key;
            //    var activationPrice = (entryPrice + takeProfitPrice) / 2;
            //    var placeTakeProfitOrderResult = await _client.UsdFuturesApi.Trading.PlaceOrderAsync(Symbol,
            //        side.Reverse(),
            //        FuturesOrderType.TrailingStopMarket,
            //        Math.Abs(position.Quantity),
            //        activationPrice: activationPrice,
            //        callbackRate: 0.5M,
            //        newClientOrderId: $"{Symbol.ToLower()}-take-profit",
            //        ct: stoppingToken);

            //    if (!placeTakeProfitOrderResult.Success)
            //    {
            //        _logger.LogError("TakeProfit result error: {Error}", placeTakeProfitOrderResult.Error);
            //    }
            //}, stoppingToken);
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
                    await PlaceStopLossOrder(orderUpdate, binanceRestClient, stoppingToken);

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
                _stopLossCount = 0;

                await trading.CancelAllOrdersAsync(orderUpdate.Symbol, ct: stoppingToken);

                return;
            }

            var oldStopLossOrder = await GetStopLossOrderAsync(position.Symbol, client, stoppingToken);
            var priceFilter = await GetSymbolPriceFilter(position.Symbol, client, stoppingToken);
            var isNewStopLossOrder = await TryPlaceNewStopLossOrderAsync(client, position, priceFilter, stoppingToken);

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

        private async Task<BinanceSymbolPriceFilter?> GetSymbolPriceFilter(string symbol,
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

            return symbolInformation.PriceFilter;
        }

        private async Task<bool> TryPlaceNewStopLossOrderAsync(BinanceRestClient client,
            BinancePositionDetailsUsdt position,
            BinanceSymbolPriceFilter? priceFilter,
            CancellationToken stoppingToken)
        {
            var trading = client.UsdFuturesApi.Trading;
            var positionSideAsOrderSide = position.Quantity.AsOrderSide();
            var stopLossMoneyQuantity = 2m;
            var sign = positionSideAsOrderSide == OrderSide.Buy ? 1 : -1;
            var positionCost = position.EntryPrice * Math.Abs(position.Quantity);
            var stopLossPrice = (positionCost - (sign * stopLossMoneyQuantity)) / Math.Abs(position.Quantity);
            var entryCommissionCost = positionCost * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / position.Quantity;
            var stopLossPriceAfterEntryCommission = stopLossPrice + entryCommissionPriceDistance;
            var lossAfterFees = 1 - sign * 0.05m / 100;
            var stopLossPriceAfterTotalFees = stopLossPriceAfterEntryCommission / lossAfterFees;

            var placeStopLossOrderResult = await trading.PlaceOrderAsync(position.Symbol,
                positionSideAsOrderSide.Reverse(),
                FuturesOrderType.StopMarket,
                quantity: null,
                stopPrice: ApplyPriceFilter(stopLossPriceAfterTotalFees, priceFilter),
                closePosition: true,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: $"{position.Symbol.ToLower()}-stop-loss-{++_stopLossCount}",
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
