using System.Globalization;
using System.Text.Json;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Futures;
using Execution.Application;
using MarketData.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingPlatform.Kernel;
using BinancePositionSide = Binance.Net.Enums.PositionSide;

namespace Execution.Infrastructure;

public sealed class BinanceLiveOrderIntentSink(
    IBinanceRestClient rest,
    IInstrumentRegistry registry,
    IOptions<LiveTradingOptions> options,
    ILogger<BinanceLiveOrderIntentSink> log) : ILiveOrderIntentSink
{
    private const string Venue = "binance";
    private const string Market = "usdm";
    private const string ContractType = "perpetual";
    private int _ordersToday;
    private DateOnly _orderDay = DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task OnIntentAsync(OrderIntent intent, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Armed)
        {
            log.LogWarning("Live order rejected: disarmed (set Armed or use --arm).");
            return;
        }

        if (opts.KillSwitchFilePath is { } kill && File.Exists(kill))
        {
            log.LogError("Live order rejected: kill-switch file present at {Path}.", kill);
            return;
        }

        ResetDailyCounterIfNeeded();
        if (_ordersToday >= opts.MaxOrdersPerDay)
        {
            log.LogError("Live order rejected: daily cap {Cap}.", opts.MaxOrdersPerDay);
            return;
        }

        if (intent.Kind is not (OrderIntentKind.OpenLong or OrderIntentKind.OpenShort))
        {
            log.LogInformation("Ignoring non-entry intent {Kind} in live sink.", intent.Kind);
            return;
        }

        if (intent.Tag is not { } tag || !tag.StartsWith("fire-test:", StringComparison.Ordinal))
        {
            log.LogWarning("Live entry requires fire-test tag with symbol (portfolio-gated path pending).");
            return;
        }

        var symbol = tag["fire-test:".Length..];
        if (!await HasPassVerdictAsync(opts.VerdictDirectory, symbol, cancellationToken).ConfigureAwait(false))
        {
            log.LogError("Live order rejected: no PASS verdict for {Symbol}.", symbol);
            return;
        }

        var instrument = await registry
            .GetByExchangeSymbolAsync(Venue, Market, ContractType, symbol, cancellationToken)
            .ConfigureAwait(false);
        if (instrument is null)
        {
            log.LogError("Instrument {Symbol} not in registry.", symbol);
            return;
        }

        var exchangeInfo = await rest.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!exchangeInfo.Success || exchangeInfo.Data is null)
        {
            log.LogError("Exchange info failed: {Err}", exchangeInfo.Error?.Message);
            return;
        }

        var sym = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
        if (sym is null)
        {
            log.LogError("Symbol {Symbol} not found on exchange.", symbol);
            return;
        }

        await rest.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, FuturesMarginType.Isolated, ct: cancellationToken)
            .ConfigureAwait(false);
        await rest.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, opts.MaxLeverage, ct: cancellationToken)
            .ConfigureAwait(false);

        var ticker = await rest.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol, cancellationToken).ConfigureAwait(false);
        if (!ticker.Success || ticker.Data is null)
        {
            log.LogError("Price failed for {Symbol}.", symbol);
            return;
        }

        var price = ticker.Data.Price;
        var qty = ComputeMarketQty(sym, price);
        var notional = qty * price;
        if (notional > opts.MaxNotionalUsdPerSymbol)
        {
            log.LogError("Reject {Symbol}: notional {N:F2} > cap {Cap:F2}.", symbol, notional, opts.MaxNotionalUsdPerSymbol);
            return;
        }

        var side = intent.Kind == OrderIntentKind.OpenLong ? OrderSide.Buy : OrderSide.Sell;
        var entry = await rest.UsdFuturesApi.Trading.PlaceOrderAsync(
            symbol,
            side,
            FuturesOrderType.Market,
            quantity: qty,
            positionSide: BinancePositionSide.Long,
            ct: cancellationToken).ConfigureAwait(false);

        if (!entry.Success)
        {
            log.LogError("Entry order failed: {Err}", entry.Error?.Message);
            return;
        }

        _ordersToday++;
        log.LogInformation("ENTRY {Symbol} qty={Qty} orderId={Id}", symbol, qty, entry.Data?.Id);

        if (intent.StopLossPrice is { } sl)
        {
            var slSide = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var slOrder = await rest.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                slSide,
                FuturesOrderType.StopMarket,
                quantity: qty,
                stopPrice: sl,
                positionSide: BinancePositionSide.Long,
                ct: cancellationToken).ConfigureAwait(false);

            if (!slOrder.Success)
                log.LogError("Stop-loss order failed: {Err}", slOrder.Error?.Message);
            else
                log.LogInformation("SL {Symbol} @ {Sl} orderId={Id}", symbol, sl, slOrder.Data?.Id);
        }
    }

    private static async Task<bool> HasPassVerdictAsync(string verdictDir, string symbol, CancellationToken ct)
    {
        var file = Path.Combine(verdictDir, $"{symbol}-4H-Rsi5Extreme.json");
        if (!File.Exists(file))
            return false;
        var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("Pass", out var p) && p.GetBoolean();
    }

    private void ResetDailyCounterIfNeeded()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today != _orderDay)
        {
            _orderDay = today;
            _ordersToday = 0;
        }
    }

    internal static decimal ComputeMarketQty(BinanceFuturesUsdtSymbol sym, decimal price)
    {
        var minQty = sym.LotSizeFilter?.MinQuantity ?? 0.001m;
        var step = sym.LotSizeFilter?.StepSize ?? 0.001m;
        var minNotional = sym.MinNotionalFilter?.MinNotional ?? 5m;
        var raw = Math.Max(minQty, minNotional / price);
        return RoundUpToStep(raw, step);
    }

    private static decimal RoundUpToStep(decimal value, decimal step)
    {
        if (step <= 0)
            return value;
        var steps = Math.Ceiling(value / step);
        return steps * step;
    }
}
