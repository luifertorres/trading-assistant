using Binance.Net;

using Binance.Net.Enums;

using Binance.Net.Interfaces.Clients;

using Binance.Net.Objects.Models.Futures;

using CryptoExchange.Net.Objects.Sockets;

using Microsoft.Extensions.Options;

using WebSocketTrading;

using BinancePositionSide = Binance.Net.Enums.PositionSide;



namespace WebSocketTrading.Worker;



public sealed class TradingWorker(

    IBinanceRestClient rest,

    IBinanceSocketClient socket,

    IOptions<TradingOptions> options,

    ILogger<TradingWorker> log) : BackgroundService

{

    private readonly TradingOptions _options = options.Value;

    private readonly object _gate = new();

    private readonly Dictionary<KlineInterval, CandleBuffer> _buffers = [];

    private readonly List<VectorRuntime> _vectors = [];



    private decimal _stepSize = 1m;

    private decimal _minQuantity = 1m;

    private decimal _minNotional = 5m;



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {

        var (symbol, intervals) = InitializeVectors();



        log.LogInformation(

            "Starting WebSocketTrading worker for {Symbol} on {Intervals} with {VectorCount} vector(s), notional {NotionalUsd} USD.",

            symbol,

            string.Join(", ", intervals),

            _vectors.Count,

            _options.NotionalUsd);



        await LoadSymbolFiltersAsync(symbol, stoppingToken).ConfigureAwait(false);

        await EnsureHedgeModeAsync(stoppingToken).ConfigureAwait(false);

        await ConfigureAccountAsync(symbol, stoppingToken).ConfigureAwait(false);

        await SyncPositionsAsync(symbol, stoppingToken).ConfigureAwait(false);



        foreach (var interval in intervals)

            await WarmupAsync(symbol, interval, stoppingToken).ConfigureAwait(false);



        var subscriptions = new List<CryptoExchange.Net.Objects.Sockets.UpdateSubscription>();

        foreach (var interval in intervals)

        {

            var subscription = await socket.UsdFuturesApi.ExchangeData

                .SubscribeToKlineUpdatesAsync(

                    symbol,

                    interval,

                    data => _ = OnKlineUpdateAsync(data, symbol, interval, stoppingToken),

                    ct: stoppingToken)

                .ConfigureAwait(false);



            if (!subscription.Success)

                throw new InvalidOperationException(

                    $"Kline subscribe failed for {interval}: {subscription.Error?.Message}");



            if (subscription.Data is not null)

                subscriptions.Add(subscription.Data);



            log.LogInformation("Subscribed to {Symbol} {Interval} kline stream.", symbol, interval);

        }



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

            foreach (var subscription in subscriptions)

                await socket.UnsubscribeAsync(subscription).ConfigureAwait(false);

        }

    }



    private (string Symbol, IReadOnlyList<KlineInterval> Intervals) InitializeVectors()

    {

        var vectors = _options.Vectors

            .Select(ToTradingVector)

            .ToList();



        var plan = TradingVectorCatalog.Build(vectors);



        foreach (var vector in plan.Vectors)

        {

            var options = _options.Vectors.First(v =>

                string.Equals(v.Asset, vector.Asset, StringComparison.Ordinal) &&

                v.Direction == vector.Direction &&

                string.Equals(v.Timeframe, vector.Timeframe, StringComparison.Ordinal) &&

                v.TradingLogic == vector.TradingLogic);



            if (options.TradingLogic != TradingLogic.Sma200Sma5)

                throw new InvalidOperationException($"Unsupported TradingLogic: {options.TradingLogic}");



            var interval = options.GetKlineInterval();

            if (!_buffers.ContainsKey(interval))

                _buffers[interval] = new CandleBuffer(Sma200Sma5TradingLogic.RequiredBars);



            _vectors.Add(new VectorRuntime(

                options,

                interval,

                new Sma200Sma5TradingLogic(options.Direction),

                new VectorInventory()));

        }



        var intervals = plan.DistinctTimeframes

            .Select(tf => Enum.Parse<KlineInterval>(tf, ignoreCase: true))

            .ToList();



        return (plan.Asset, intervals);

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



    private async Task EnsureHedgeModeAsync(CancellationToken cancellationToken)

    {

        var mode = await rest.UsdFuturesApi.Account

            .GetPositionModeAsync(ct: cancellationToken)

            .ConfigureAwait(false);



        if (!mode.Success || mode.Data is null)

            throw new InvalidOperationException($"Get position mode failed: {mode.Error?.Message}");



        if (mode.Data.IsHedgeMode)

        {

            log.LogInformation("Account already in hedge (dual-side) position mode.");

            return;

        }



        var change = await rest.UsdFuturesApi.Account

            .ModifyPositionModeAsync(true, ct: cancellationToken)

            .ConfigureAwait(false);



        if (!change.Success)

            throw new InvalidOperationException($"Enable hedge mode failed: {change.Error?.Message}");



        log.LogInformation("Enabled hedge (dual-side) position mode for USD-M Futures.");

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



    private async Task SyncPositionsAsync(string symbol, CancellationToken cancellationToken)

    {

        var positions = await rest.UsdFuturesApi.Account

            .GetPositionInformationAsync(symbol, ct: cancellationToken)

            .ConfigureAwait(false);



        if (!positions.Success || positions.Data is null)

            throw new InvalidOperationException($"Position sync failed: {positions.Error?.Message}");



        lock (_gate)

        {

            foreach (var directionGroup in _vectors.GroupBy(v => v.Options.Direction))

            {

                var direction = directionGroup.Key;

                var binanceSide = ToBinancePositionSide(direction);

                var sideQty = GetSideQuantity(positions.Data, symbol, binanceSide);

                var runtimes = directionGroup.ToList();



                if (runtimes.Count > 1 && sideQty > 0)

                {

                    log.LogWarning(

                        "Multiple {Direction} vectors share {Symbol}; seeding {Quantity} to first vector ({Timeframe}). Others start at zero.",

                        direction,

                        symbol,

                        sideQty,

                        runtimes[0].Options.Timeframe);

                }



                var seeded = false;

                foreach (var runtime in runtimes)

                {

                    if (!seeded && sideQty > 0)

                    {

                        runtime.Inventory.Seed(sideQty);

                        runtime.Position = direction == Direction.Long

                            ? PositionState.Long

                            : PositionState.Short;

                        seeded = true;

                        continue;

                    }



                    runtime.Inventory.Seed(0m);

                    runtime.Position = PositionState.OutOfMarket;

                }

            }

        }



        foreach (var runtime in _vectors)

        {

            log.LogInformation(

                "Startup position for {Symbol} {Direction} {Timeframe}: state={State}, trackedQty={TrackedQty}.",

                symbol,

                runtime.Options.Direction,

                runtime.Options.Timeframe,

                runtime.Position,

                runtime.Inventory.OpenQuantity);

        }

    }



    private async Task WarmupAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken)

    {

        var result = await rest.UsdFuturesApi.ExchangeData

            .GetKlinesAsync(

                symbol,

                interval,

                limit: Sma200Sma5TradingLogic.RequiredBars,

                ct: cancellationToken)

            .ConfigureAwait(false);



        if (!result.Success || result.Data is null)

            throw new InvalidOperationException($"Warmup klines failed for {interval}: {result.Error?.Message}");



        var closed = result.Data

            .Where(k => k.CloseTime <= DateTime.UtcNow)

            .OrderBy(k => k.OpenTime)

            .Select(BinanceKlineMapping.ToCandle)

            .ToList();



        lock (_gate)

        {

            _buffers[interval].AddRange(closed);

        }



        log.LogInformation(

            "Warmup loaded {Count} closed candles for {Symbol} {Interval}.",

            _buffers[interval].Candles.Count,

            symbol,

            interval);

    }



    private async Task OnKlineUpdateAsync(

        DataEvent<Binance.Net.Interfaces.IBinanceStreamKlineData> update,

        string symbol,

        KlineInterval interval,

        CancellationToken cancellationToken)

    {

        try

        {

            var kline = update.Data.Data;

            if (!kline.Final)

                return;



            var candle = BinanceKlineMapping.ToCandle(kline);

            List<(VectorRuntime Runtime, TradeAction Action)> actions;



            lock (_gate)

            {

                var buffer = _buffers[interval];

                buffer.Add(candle);

                actions = _vectors

                    .Where(runtime => runtime.Interval == interval)

                    .Select(runtime => (runtime, runtime.Logic.Evaluate(buffer.Candles, runtime.Position)))

                    .ToList();

            }



            foreach (var (runtime, action) in actions)

            {

                log.LogInformation(

                    "Closed candle {Symbol} {Timeframe} @ {OpenTime:u} close={Close} direction={Direction} action={Action} position={Position}.",

                    symbol,

                    runtime.Options.Timeframe,

                    candle.Date,

                    candle.Close,

                    runtime.Options.Direction,

                    action,

                    runtime.Position);



                if (action == TradeAction.Hold)

                    continue;



                await ExecuteActionAsync(runtime, symbol, candle.Close, action, cancellationToken)

                    .ConfigureAwait(false);



                if (action is TradeAction.ExitShort or TradeAction.ExitLong)

                {

                    TradeAction followUp;

                    lock (_gate)

                    {

                        var buffer = _buffers[interval];

                        followUp = runtime.Logic.Evaluate(buffer.Candles, PositionState.OutOfMarket);

                    }



                    if (followUp is TradeAction.EnterShort or TradeAction.EnterLong)

                    {

                        await ExecuteActionAsync(runtime, symbol, candle.Close, followUp, cancellationToken)

                            .ConfigureAwait(false);

                    }

                }

            }

        }

        catch (Exception ex) when (ex is not OperationCanceledException)

        {

            log.LogError(ex, "Error handling kline update for {Symbol} {Interval}.", symbol, interval);

        }

    }



    private async Task ExecuteActionAsync(

        VectorRuntime runtime,

        string symbol,

        decimal price,

        TradeAction action,

        CancellationToken cancellationToken)

    {

        var binanceSide = ToBinancePositionSide(runtime.Options.Direction);



        if (action is TradeAction.EnterShort or TradeAction.EnterLong)

        {

            var quantity = QuantitySizer.SizeFromNotional(

                _options.NotionalUsd,

                price,

                _stepSize,

                _minQuantity,

                _minNotional);



            var side = action == TradeAction.EnterLong ? OrderSide.Buy : OrderSide.Sell;

            var order = await socket.UsdFuturesApi.Trading

                .PlaceOrderAsync(

                    symbol,

                    side,

                    FuturesOrderType.Market,

                    quantity: quantity,

                    positionSide: binanceSide,

                    ct: cancellationToken)

                .ConfigureAwait(false);



            if (!order.Success)

            {

                log.LogError(

                    "Enter {Direction} {Timeframe} failed: {Message}",

                    runtime.Options.Direction,

                    runtime.Options.Timeframe,

                    order.Error?.Message);

                return;

            }



            lock (_gate)

            {

                runtime.Inventory.AddFill(quantity);

                runtime.Position = runtime.Options.Direction == Direction.Long

                    ? PositionState.Long

                    : PositionState.Short;

            }



            log.LogInformation(

                "Entered {Direction} {Symbol} {Timeframe} qty={Quantity} trackedQty={TrackedQty} orderId={OrderId}.",

                runtime.Options.Direction,

                symbol,

                runtime.Options.Timeframe,

                quantity,

                runtime.Inventory.OpenQuantity,

                order.Data?.Id);

            return;

        }



        if (action is TradeAction.ExitShort or TradeAction.ExitLong)

        {

            decimal trackedQty;

            lock (_gate)

            {

                trackedQty = runtime.Inventory.OpenQuantity;

            }



            if (trackedQty <= 0)

            {

                log.LogWarning(

                    "Exit {Direction} {Timeframe} skipped: no tracked quantity for {Symbol}.",

                    runtime.Options.Direction,

                    runtime.Options.Timeframe,

                    symbol);



                lock (_gate)

                {

                    runtime.Position = PositionState.OutOfMarket;

                }



                return;

            }



            var exchangeSideQty = await GetSideQuantityAsync(symbol, binanceSide, cancellationToken)

                .ConfigureAwait(false);



            if (exchangeSideQty <= 0)

            {

                log.LogWarning(

                    "Exit {Direction} {Timeframe} skipped: no exchange quantity for {Symbol}.",

                    runtime.Options.Direction,

                    runtime.Options.Timeframe,

                    symbol);



                lock (_gate)

                {

                    runtime.Inventory.ConsumeForExit();

                    runtime.Position = PositionState.OutOfMarket;

                }



                return;

            }



            var exitQty = Math.Min(trackedQty, exchangeSideQty);

            var side = action == TradeAction.ExitLong ? OrderSide.Sell : OrderSide.Buy;

            var order = await socket.UsdFuturesApi.Trading

                .PlaceOrderAsync(

                    symbol,

                    side,

                    FuturesOrderType.Market,

                    quantity: exitQty,

                    positionSide: binanceSide,

                    reduceOnly: true,

                    ct: cancellationToken)

                .ConfigureAwait(false);



            if (!order.Success)

            {

                log.LogError(

                    "Exit {Direction} {Timeframe} failed: {Message}",

                    runtime.Options.Direction,

                    runtime.Options.Timeframe,

                    order.Error?.Message);

                return;

            }



            lock (_gate)

            {

                runtime.Inventory.ConsumeForExit();

                runtime.Position = PositionState.OutOfMarket;

            }



            log.LogInformation(

                "Exited {Direction} {Symbol} {Timeframe} qty={Quantity} orderId={OrderId}.",

                runtime.Options.Direction,

                symbol,

                runtime.Options.Timeframe,

                exitQty,

                order.Data?.Id);

        }

    }



    private async Task<decimal> GetSideQuantityAsync(

        string symbol,

        BinancePositionSide positionSide,

        CancellationToken cancellationToken)

    {

        var positions = await rest.UsdFuturesApi.Account

            .GetPositionInformationAsync(symbol, ct: cancellationToken)

            .ConfigureAwait(false);



        if (!positions.Success || positions.Data is null)

            throw new InvalidOperationException($"Position query failed: {positions.Error?.Message}");



        return GetSideQuantity(positions.Data, symbol, positionSide);

    }



    private static decimal GetSideQuantity(

        IEnumerable<BinancePositionDetailsUsdt> positions,

        string symbol,

        BinancePositionSide positionSide)

    {

        var position = positions.FirstOrDefault(p =>

            p.Symbol == symbol && p.PositionSide == positionSide);



        return position?.Quantity is { } quantity ? Math.Abs(quantity) : 0m;

    }



    private static TradingVector ToTradingVector(TradingVectorOptions options) =>

        new(options.Asset, options.Direction, options.Timeframe, options.TradingLogic);



    private static BinancePositionSide ToBinancePositionSide(Direction direction) =>

        direction == Direction.Long ? BinancePositionSide.Long : BinancePositionSide.Short;



    private sealed class VectorRuntime(

        TradingVectorOptions options,

        KlineInterval interval,

        Sma200Sma5TradingLogic logic,

        VectorInventory inventory)

    {

        public TradingVectorOptions Options { get; } = options;



        public KlineInterval Interval { get; } = interval;



        public Sma200Sma5TradingLogic Logic { get; } = logic;



        public VectorInventory Inventory { get; } = inventory;



        public PositionState Position { get; set; } = PositionState.OutOfMarket;

    }

}


