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
    private readonly Dictionary<(string Symbol, KlineInterval Interval), CandleBuffer> _buffers = [];
    private readonly Dictionary<string, SymbolFilters> _symbolFilters = new(StringComparer.Ordinal);
    private readonly List<VectorRuntime> _vectors = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation(
            "Trading config: universeEnabled={UniverseEnabled}, timeframe={Timeframe}, manualVectorCount={ManualVectorCount}.",
            _options.Universe.Enabled,
            _options.Universe.Timeframe,
            _options.Vectors.Count);

        var plan = await InitializeVectorsAsync(stoppingToken).ConfigureAwait(false);

        log.LogInformation(
            "Starting WebSocketTrading worker for {SymbolCount} symbol(s) on {Intervals} with {VectorCount} vector(s), notional {NotionalUsd} USD (max {MaxNotionalUsd} USD).",
            plan.Assets.Count,
            string.Join(", ", plan.DistinctTimeframes),
            _vectors.Count,
            _options.NotionalUsd,
            _options.MaxNotionalUsd);

        await LoadSymbolFiltersAsync(plan.Assets, stoppingToken).ConfigureAwait(false);
        await EnsureHedgeModeAsync(stoppingToken).ConfigureAwait(false);

        foreach (var asset in plan.Assets)
            await ConfigureAccountAsync(asset, stoppingToken).ConfigureAwait(false);

        await SyncAllPositionsAsync(stoppingToken).ConfigureAwait(false);

        var warmupCount = 0;
        foreach (var assetTimeframe in plan.DistinctAssetTimeframes)
        {
            warmupCount++;
            var interval = Enum.Parse<KlineInterval>(assetTimeframe.Timeframe, ignoreCase: true);
            await WarmupAsync(assetTimeframe.Asset, interval, stoppingToken).ConfigureAwait(false);

            if (warmupCount % 25 == 0)
            {
                log.LogInformation(
                    "Warmup progress: {Completed}/{Total} symbol/timeframe pairs.",
                    warmupCount,
                    plan.DistinctAssetTimeframes.Count);
            }
        }

        var subscriptions = new List<UpdateSubscription>();
        foreach (var assetTimeframe in plan.DistinctAssetTimeframes)
        {
            var interval = Enum.Parse<KlineInterval>(assetTimeframe.Timeframe, ignoreCase: true);
            var subscription = await socket.UsdFuturesApi.ExchangeData
                .SubscribeToKlineUpdatesAsync(
                    assetTimeframe.Asset,
                    interval,
                    data => _ = OnKlineUpdateAsync(data, assetTimeframe.Asset, interval, stoppingToken),
                    ct: stoppingToken)
                .ConfigureAwait(false);

            if (!subscription.Success)
            {
                throw new InvalidOperationException(
                    $"Kline subscribe failed for {assetTimeframe.Asset} {interval}: {subscription.Error?.Message}");
            }

            if (subscription.Data is not null)
                subscriptions.Add(subscription.Data);

            log.LogInformation(
                "Subscribed to {Symbol} {Interval} kline stream.",
                assetTimeframe.Asset,
                interval);
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

    private async Task<TradingVectorPlan> InitializeVectorsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TradingVector> vectors;
        if (_options.Universe.Enabled)
        {
            var symbols = await DiscoverEligibleSymbolsAsync(cancellationToken).ConfigureAwait(false);
            vectors = TradingUniverseFactory.BuildLongShort(
                symbols,
                _options.Universe.Timeframe,
                _options.Universe.TradingLogic);

            log.LogInformation(
                "Universe mode enabled: {SymbolCount} eligible USDT perpetual(s), {VectorCount} vector(s).",
                symbols.Count,
                vectors.Count);
        }
        else
        {
            if (_options.Vectors.Count == 0)
            {
                throw new InvalidOperationException(
                    "Trading:Vectors must be configured when Trading:Universe:Enabled is false.");
            }

            vectors = _options.Vectors.Select(ToTradingVector).ToList();
        }

        var plan = TradingVectorCatalog.Build(vectors);

        foreach (var vector in plan.Vectors)
        {
            if (vector.TradingLogic != TradingLogic.Sma200Sma5)
                throw new InvalidOperationException($"Unsupported TradingLogic: {vector.TradingLogic}");

            var vectorOptions = TradingVectorOptions.FromVector(vector);
            var interval = vectorOptions.GetKlineInterval();
            var bufferKey = (vector.Asset, interval);

            if (!_buffers.ContainsKey(bufferKey))
                _buffers[bufferKey] = new CandleBuffer(Sma200Sma5TradingLogic.RequiredBars);

            _vectors.Add(new VectorRuntime(
                vectorOptions,
                interval,
                new Sma200Sma5TradingLogic(vector.Direction),
                new VectorInventory()));
        }

        return plan;
    }

    private async Task<IReadOnlyList<string>> DiscoverEligibleSymbolsAsync(CancellationToken cancellationToken)
    {
        var exchangeInfo = await rest.UsdFuturesApi.ExchangeData
            .GetExchangeInfoAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!exchangeInfo.Success || exchangeInfo.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {exchangeInfo.Error?.Message}");

        var tickers = await rest.UsdFuturesApi.ExchangeData
            .GetTickersAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!tickers.Success || tickers.Data is null)
            throw new InvalidOperationException($"Ticker prices failed: {tickers.Error?.Message}");

        var prices = tickers.Data.ToDictionary(t => t.Symbol, t => t.LastPrice, StringComparer.Ordinal);

        var eligible = new List<string>();
        foreach (var symbol in exchangeInfo.Data.Symbols
                     .Where(s => s.Status == SymbolStatus.Trading)
                     .Where(s => s.ContractType == ContractType.Perpetual)
                     .Where(s => string.Equals(s.QuoteAsset, "USDT", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            if (!prices.TryGetValue(symbol.Name, out var price) || price <= 0m)
            {
                log.LogWarning("Skipping {Symbol}: no usable last price for notional fit.", symbol.Name);
                continue;
            }

            var filters = new SymbolFilters(
                symbol.MarketLotSizeFilter?.StepSize ?? symbol.LotSizeFilter?.StepSize ?? 1m,
                symbol.MarketLotSizeFilter?.MinQuantity
                    ?? symbol.LotSizeFilter?.MinQuantity
                    ?? symbol.MarketLotSizeFilter?.StepSize
                    ?? symbol.LotSizeFilter?.StepSize
                    ?? 1m,
                symbol.MinNotionalFilter?.MinNotional ?? 5m);
            if (SymbolNotionalFit.Fits(
                    _options.NotionalUsd,
                    _options.MaxNotionalUsd,
                    price,
                    filters.StepSize,
                    filters.MinQuantity,
                    filters.MinNotional))
            {
                eligible.Add(symbol.Name);
                continue;
            }

            log.LogDebug(
                "Skipping {Symbol}: minimum sized entry exceeds max notional {MaxNotionalUsd} USD.",
                symbol.Name,
                _options.MaxNotionalUsd);
        }

        if (eligible.Count == 0)
        {
            throw new InvalidOperationException(
                "Universe discovery found no eligible USDT perpetual symbols for the configured notional caps.");
        }

        return eligible;
    }

    private async Task LoadSymbolFiltersAsync(
        IReadOnlyList<string> symbols,
        CancellationToken cancellationToken)
    {
        var exchangeInfo = await rest.UsdFuturesApi.ExchangeData
            .GetExchangeInfoAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!exchangeInfo.Success || exchangeInfo.Data is null)
            throw new InvalidOperationException($"Exchange info failed: {exchangeInfo.Error?.Message}");

        var symbolSet = symbols.ToHashSet(StringComparer.Ordinal);
        foreach (var sym in exchangeInfo.Data.Symbols.Where(s => symbolSet.Contains(s.Name)))
        {
            var filters = new SymbolFilters(
                sym.MarketLotSizeFilter?.StepSize ?? sym.LotSizeFilter?.StepSize ?? 1m,
                sym.MarketLotSizeFilter?.MinQuantity
                    ?? sym.LotSizeFilter?.MinQuantity
                    ?? sym.MarketLotSizeFilter?.StepSize
                    ?? sym.LotSizeFilter?.StepSize
                    ?? 1m,
                sym.MinNotionalFilter?.MinNotional ?? 5m);
            _symbolFilters[sym.Name] = filters;

            log.LogInformation(
                "Loaded filters for {Symbol}: step={StepSize}, minQty={MinQuantity}, minNotional={MinNotional}.",
                sym.Name,
                filters.StepSize,
                filters.MinQuantity,
                filters.MinNotional);
        }

        var missing = symbolSet.Except(_symbolFilters.Keys, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Exchange info missing filters for: {string.Join(", ", missing)}");
        }
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
            log.LogWarning("Change margin type for {Symbol}: {Message}", symbol, margin.Error?.Message);

        var leverage = await rest.UsdFuturesApi.Account
            .ChangeInitialLeverageAsync(symbol, _options.Leverage, ct: cancellationToken)
            .ConfigureAwait(false);

        if (!leverage.Success)
            throw new InvalidOperationException($"Change leverage failed for {symbol}: {leverage.Error?.Message}");

        log.LogInformation("Configured {Symbol} to isolated margin at {Leverage}x leverage.", symbol, _options.Leverage);
    }

    private async Task SyncAllPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = await rest.UsdFuturesApi.Account
            .GetPositionInformationAsync(ct: cancellationToken)
            .ConfigureAwait(false);

        if (!positions.Success || positions.Data is null)
            throw new InvalidOperationException($"Position sync failed: {positions.Error?.Message}");

        lock (_gate)
        {
            foreach (var assetGroup in _vectors.GroupBy(v => v.Options.Asset, StringComparer.Ordinal))
            {
                var symbol = assetGroup.Key;
                foreach (var directionGroup in assetGroup.GroupBy(v => v.Options.Direction))
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
        }

        foreach (var runtime in _vectors)
        {
            log.LogInformation(
                "Startup position for {Symbol} {Direction} {Timeframe}: state={State}, trackedQty={TrackedQty}.",
                runtime.Options.Asset,
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
            throw new InvalidOperationException($"Warmup klines failed for {symbol} {interval}: {result.Error?.Message}");

        var closed = result.Data
            .Where(k => k.CloseTime <= DateTime.UtcNow)
            .OrderBy(k => k.OpenTime)
            .Select(BinanceKlineMapping.ToCandle)
            .ToList();

        lock (_gate)
        {
            _buffers[(symbol, interval)].AddRange(closed);
        }

        log.LogInformation(
            "Warmup loaded {Count} closed candles for {Symbol} {Interval}.",
            closed.Count,
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
                var buffer = _buffers[(symbol, interval)];
                buffer.Add(candle);
                actions = _vectors
                    .Where(runtime =>
                        string.Equals(runtime.Options.Asset, symbol, StringComparison.Ordinal) &&
                        runtime.Interval == interval)
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
                    PositionState positionAfterExit;
                    lock (_gate)
                    {
                        positionAfterExit = runtime.Position;
                    }

                    if (positionAfterExit == PositionState.OutOfMarket)
                    {
                        TradeAction followUp;
                        lock (_gate)
                        {
                            var buffer = _buffers[(symbol, interval)];
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
        var filters = _symbolFilters[symbol];

        if (action is TradeAction.EnterShort or TradeAction.EnterLong)
        {
            if (!EntryNotionalGuard.TrySize(
                    _options.NotionalUsd,
                    _options.MaxNotionalUsd,
                    price,
                    filters.StepSize,
                    filters.MinQuantity,
                    filters.MinNotional,
                    out var quantity))
            {
                log.LogWarning(
                    "Enter {Direction} {Timeframe} skipped for {Symbol}: sized notional exceeds max {MaxNotionalUsd} USD.",
                    runtime.Options.Direction,
                    runtime.Options.Timeframe,
                    symbol,
                    _options.MaxNotionalUsd);
                return;
            }

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
                    reduceOnly: BinanceUsdMOrderRules.ReduceOnlyParameter(hedgeMode: true),
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
