using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using MarketData.Application;
using Microsoft.Extensions.Logging;
using TradingPlatform.Kernel;

namespace MarketData.Infrastructure;

public sealed class BinanceLiveCandleFeed(
    IBinanceRestClient rest,
    IBinanceSocketClient socket,
    IInstrumentRegistry registry,
    ILogger<BinanceLiveCandleFeed> log) : ILiveCandleFeed
{
    private const string Venue = "binance";
    private const string Market = "usdm";
    private const string ContractType = "perpetual";

    public async Task SubscribeAsync(
        IReadOnlyList<string> exchangeSymbols,
        TimeFrameCode timeFrame,
        Func<ClosedCandleEvent, CancellationToken, Task> onClosedCandle,
        CancellationToken cancellationToken = default)
    {
        var interval = BackfillKlineIntervalMapping.ToBinance(timeFrame);
        var symbolMap = await ResolveInstrumentsAsync(exchangeSymbols, cancellationToken).ConfigureAwait(false);

        foreach (var symbol in exchangeSymbols)
        {
            var result = await socket.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(
                symbol,
                interval,
                async data =>
                {
                    var kline = data.Data.Data;
                    if (!kline.Final)
                        return;

                    if (!symbolMap.TryGetValue(symbol, out var instrumentId))
                        return;

                    var bar = BinanceKlineMapping.ToOhlcBar(kline);
                    await onClosedCandle(
                        new ClosedCandleEvent(instrumentId, symbol, timeFrame, bar),
                        cancellationToken).ConfigureAwait(false);
                },
                ct: cancellationToken).ConfigureAwait(false);

            if (!result.Success)
                throw new InvalidOperationException($"Kline subscribe failed for {symbol}: {result.Error?.Message}");
        }

        log.LogInformation("Subscribed to {Count} symbols on {TimeFrame} kline feed.", exchangeSymbols.Count, timeFrame.Value);
    }

    public async Task ReplayLastClosedAsync(
        IReadOnlyList<string> exchangeSymbols,
        TimeFrameCode timeFrame,
        Func<ClosedCandleEvent, CancellationToken, Task> onClosedCandle,
        CancellationToken cancellationToken = default)
    {
        var interval = BackfillKlineIntervalMapping.ToBinance(timeFrame);
        var symbolMap = await ResolveInstrumentsAsync(exchangeSymbols, cancellationToken).ConfigureAwait(false);

        foreach (var symbol in exchangeSymbols)
        {
            if (!symbolMap.TryGetValue(symbol, out var instrumentId))
                continue;

            var result = await rest.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                symbol,
                interval,
                limit: 3,
                ct: cancellationToken).ConfigureAwait(false);

            if (!result.Success || result.Data is null)
            {
                log.LogWarning("Replay: no klines for {Symbol}.", symbol);
                continue;
            }

            var klines = result.Data.OrderBy(k => k.OpenTime).ToList();
            var closed = klines.Count >= 2 ? klines[^2] : klines[^1];
            if (DateTime.UtcNow < closed.CloseTime)
                closed = klines.Count >= 2 ? klines[^2] : closed;

            var bar = BinanceKlineMapping.ToOhlcBar(closed);
            log.LogInformation("Replay last closed {Symbol} {TimeFrame} @ {CloseTime}.", symbol, timeFrame.Value, bar.CloseTime);
            await onClosedCandle(new ClosedCandleEvent(instrumentId, symbol, timeFrame, bar), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, InstrumentId>> ResolveInstrumentsAsync(
        IReadOnlyList<string> symbols,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, InstrumentId>(StringComparer.Ordinal);
        foreach (var symbol in symbols)
        {
            var inst = await registry
                .GetByExchangeSymbolAsync(Venue, Market, ContractType, symbol, cancellationToken)
                .ConfigureAwait(false);
            if (inst is not null)
                map[symbol] = inst.Id;
        }

        return map;
    }
}
