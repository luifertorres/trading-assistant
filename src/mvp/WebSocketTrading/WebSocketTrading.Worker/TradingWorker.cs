using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Microsoft.Extensions.Options;
using WebSocketTrading;

namespace WebSocketTrading.Worker;

public sealed class TradingWorker(
    IBinanceRestClient rest,
    IBinanceSocketClient socket,
    IOptions<TradingOptions> options,
    ILogger<TradingWorker> log) : BackgroundService
{
    private readonly TradingOptions _options = options.Value;
    private readonly SmaShortStrategy _strategy = new();
    private readonly CandleBuffer _buffer = new(SmaShortStrategy.RequiredBars);
    private readonly object _gate = new();

    private PositionState _position = PositionState.Flat;
    private decimal _stepSize = 1m;
    private decimal _minQuantity = 1m;
    private decimal _minNotional = 5m;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbol = _options.Symbol;
        var interval = _options.GetKlineInterval();

        log.LogInformation(
            "Starting WebSocketTrading worker for {Symbol} on {Interval} with notional {NotionalUsd} USD.",
            symbol,
            interval,
            _options.NotionalUsd);

        await LoadSymbolFiltersAsync(symbol, stoppingToken).ConfigureAwait(false);
        await ConfigureAccountAsync(symbol, stoppingToken).ConfigureAwait(false);
        await SyncPositionAsync(symbol, stoppingToken).ConfigureAwait(false);
        await WarmupAsync(symbol, interval, stoppingToken).ConfigureAwait(false);

        var subscription = await socket.UsdFuturesApi.ExchangeData
            .SubscribeToKlineUpdatesAsync(
                symbol,
                interval,
                data => _ = OnKlineUpdateAsync(data, symbol, stoppingToken),
                ct: stoppingToken)
            .ConfigureAwait(false);

        if (!subscription.Success)
            throw new InvalidOperationException($"Kline subscribe failed: {subscription.Error?.Message}");

        log.LogInformation("Subscribed to {Symbol} {Interval} kline stream.", symbol, interval);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            log.LogInformation("Stopping WebSocketTrading worker.");
        }
        finally
        {
            if (subscription.Data is not null)
                await socket.UnsubscribeAsync(subscription.Data).ConfigureAwait(false);
        }
    }

    private async Task LoadSymbolFiltersAsync(string symbol, CancellationToken cancellationToken)
    {
        var exchangeInfo = await rest.UsdFuturesApi.ExchangeData
            .GetExchangeInfoAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!exchangeInfo.Success || exchangeInfo.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {exchangeInfo.Error?.Message}");

        var sym = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol)
            ?? throw new InvalidOperationException($"Symbol {symbol} not found on USD-M Futures.");

        _stepSize = sym.MarketLotSizeFilter?.StepSize ?? sym.LotSizeFilter?.StepSize ?? 1m;
        _minQuantity = sym.MarketLotSizeFilter?.MinQuantity ?? sym.LotSizeFilter?.MinQuantity ?? _stepSize;
        _minNotional = sym.MinNotionalFilter?.MinNotional ?? 5m;

        log.LogInformation(
            "Loaded filters for {Symbol}: step={StepSize}, minQty={MinQuantity}, minNotional={MinNotional}.",
            symbol,
            _stepSize,
            _minQuantity,
            _minNotional);
    }

    private async Task ConfigureAccountAsync(string symbol, CancellationToken cancellationToken)
    {
        var margin = await rest.UsdFuturesApi.Account
            .ChangeMarginTypeAsync(symbol, FuturesMarginType.Isolated, ct: cancellationToken)
            .ConfigureAwait(false);

        if (!margin.Success && margin.Error?.Message?.Contains("No need to change", StringComparison.OrdinalIgnoreCase) != true)
            log.LogWarning("Change margin type: {Message}", margin.Error?.Message);

        var leverage = await rest.UsdFuturesApi.Account
            .ChangeInitialLeverageAsync(symbol, _options.Leverage, ct: cancellationToken)
            .ConfigureAwait(false);

        if (!leverage.Success)
            throw new InvalidOperationException($"Change leverage failed: {leverage.Error?.Message}");

        log.LogInformation("Configured {Symbol} to isolated margin at {Leverage}x leverage.", symbol, _options.Leverage);
    }

    private async Task SyncPositionAsync(string symbol, CancellationToken cancellationToken)
    {
        var positions = await rest.UsdFuturesApi.Account
            .GetPositionInformationAsync(symbol, ct: cancellationToken)
            .ConfigureAwait(false);

        if (!positions.Success || positions.Data is null)
            throw new InvalidOperationException($"Position sync failed: {positions.Error?.Message}");

        var position = positions.Data.FirstOrDefault(p => p.Symbol == symbol);
        var quantity = position?.Quantity ?? 0m;

        lock (_gate)
        {
            _position = quantity < 0 ? PositionState.Short : PositionState.Flat;
        }

        log.LogInformation("Startup position for {Symbol}: qty={Quantity}, state={State}.", symbol, quantity, _position);
    }

    private async Task WarmupAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken)
    {
        var result = await rest.UsdFuturesApi.ExchangeData
            .GetKlinesAsync(
                symbol,
                interval,
                limit: SmaShortStrategy.RequiredBars,
                ct: cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success || result.Data is null)
            throw new InvalidOperationException($"Warmup klines failed: {result.Error?.Message}");

        var closed = result.Data
            .Where(k => k.CloseTime <= DateTime.UtcNow)
            .OrderBy(k => k.OpenTime)
            .Select(BinanceKlineMapping.ToCandle)
            .ToList();

        lock (_gate)
        {
            _buffer.AddRange(closed);
        }

        log.LogInformation("Warmup loaded {Count} closed candles for {Symbol}.", _buffer.Candles.Count, symbol);
    }

    private async Task OnKlineUpdateAsync(
        CryptoExchange.Net.Objects.Sockets.DataEvent<Binance.Net.Interfaces.IBinanceStreamKlineData> update,
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            var kline = update.Data.Data;
            if (!kline.Final)
                return;

            var candle = BinanceKlineMapping.ToCandle(kline);
            TradeAction action;

            lock (_gate)
            {
                _buffer.Add(candle);
                action = _strategy.Evaluate(_buffer.Candles, _position);
            }

            log.LogInformation(
                "Closed candle {Symbol} @ {OpenTime:u} close={Close} action={Action} position={Position}.",
                symbol,
                candle.Date,
                candle.Close,
                action,
                _position);

            if (action == TradeAction.Hold)
                return;

            await ExecuteActionAsync(symbol, candle.Close, action, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Error handling kline update for {Symbol}.", symbol);
        }
    }

    private async Task ExecuteActionAsync(
        string symbol,
        decimal price,
        TradeAction action,
        CancellationToken cancellationToken)
    {
        var quantity = QuantitySizer.SizeFromNotional(
            _options.NotionalUsd,
            price,
            _stepSize,
            _minQuantity,
            _minNotional);

        if (action == TradeAction.EnterShort)
        {
            var order = await socket.UsdFuturesApi.Trading
                .PlaceOrderAsync(
                    symbol,
                    OrderSide.Sell,
                    FuturesOrderType.Market,
                    quantity: quantity,
                    ct: cancellationToken)
                .ConfigureAwait(false);

            if (!order.Success)
            {
                log.LogError("Enter short failed: {Message}", order.Error?.Message);
                return;
            }

            lock (_gate)
            {
                _position = PositionState.Short;
            }

            log.LogInformation("Entered SHORT {Symbol} qty={Quantity} orderId={OrderId}.", symbol, quantity, order.Data?.Id);
            return;
        }

        if (action == TradeAction.ExitShort)
        {
            var positionQty = await GetShortQuantityAsync(symbol, cancellationToken).ConfigureAwait(false);
            if (positionQty <= 0)
            {
                log.LogWarning("Exit short skipped: no open short quantity for {Symbol}.", symbol);
                lock (_gate)
                {
                    _position = PositionState.Flat;
                }

                return;
            }

            var order = await socket.UsdFuturesApi.Trading
                .PlaceOrderAsync(
                    symbol,
                    OrderSide.Buy,
                    FuturesOrderType.Market,
                    quantity: positionQty,
                    reduceOnly: true,
                    ct: cancellationToken)
                .ConfigureAwait(false);

            if (!order.Success)
            {
                log.LogError("Exit short failed: {Message}", order.Error?.Message);
                return;
            }

            lock (_gate)
            {
                _position = PositionState.Flat;
            }

            log.LogInformation("Exited SHORT {Symbol} qty={Quantity} orderId={OrderId}.", symbol, positionQty, order.Data?.Id);
        }
    }

    private async Task<decimal> GetShortQuantityAsync(string symbol, CancellationToken cancellationToken)
    {
        var positions = await rest.UsdFuturesApi.Account
            .GetPositionInformationAsync(symbol, ct: cancellationToken)
            .ConfigureAwait(false);

        if (!positions.Success || positions.Data is null)
            throw new InvalidOperationException($"Position query failed: {positions.Error?.Message}");

        var position = positions.Data.FirstOrDefault(p => p.Symbol == symbol);
        var quantity = position?.Quantity ?? 0m;
        return quantity < 0 ? Math.Abs(quantity) : 0m;
    }
}
