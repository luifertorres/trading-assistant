using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using MediatR;

namespace TradingAssistant
{
    public class TradeHandler : IRequestHandler<TradeRequest, bool>
    {
        private const decimal BitcoinQuantityToReduce = 0.001m;
        private readonly ILogger<TradeHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly BinanceService _binance;
        private readonly KlineInterval _timeFrame;

        public TradeHandler(ILogger<TradeHandler> logger,
            IConfiguration configuration,
            IServiceScopeFactory factory,
            FasterKV<CandleId, Candle> cache,
            BinanceService binance)
        {
            _logger = logger;
            _configuration = configuration;
            _factory = factory;
            _cache = cache;
            _binance = binance;

            _timeFrame = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
        }

        public async Task<bool> Handle(TradeRequest trade, CancellationToken cancellationToken)
        {
            var symbolToTrade = trade.Symbol;


            if (!_binance.TryGetSymbolInformation(symbolToTrade, out var information))
            {
                return false;
            }

            if (!_binance.TryGetLeverage(symbolToTrade, out var leverage))
            {
                return false;
            }

            var lastCandleId = new CandleId(symbolToTrade, _timeFrame, trade.Time);
            var lookbackPeriods = Math.Max(60 * 60 * 24 / (int)trade.TimeFrame, 2);
            var candlestick = GetCandlestick(lastCandleId, lookbackPeriods);
            var bitcoin = GetCandlestick(lastCandleId with { Symbol = "BTCUSDT" }, lookbackPeriods);
            var withoutQuoteAsset = ..^4;


            if (symbolToTrade[withoutQuoteAsset] is not "BTC" && candlestick.IsCorrelatedWith(bitcoin))
            {
                return false;
            }

            var account = await _binance.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return false;
            }

            var positions = account.Positions;
            var openPositions = positions.Where(IsPositionOpen);

            if (openPositions.Any())
            {
                return false;
            }

            var sameBaseAssetOpenPositions = openPositions.Where(p => p.Symbol[withoutQuoteAsset] == symbolToTrade[withoutQuoteAsset]);
            var newPositionSide = trade.Side;
            var sameBaseAssetSameSideOpenPositions = sameBaseAssetOpenPositions.Where(p => p.Quantity.AsOrderSide() == newPositionSide);

            if (sameBaseAssetSameSideOpenPositions.Any())
            {
                return false;
            }

            var sameSymbolOpenPositions = openPositions.Where(p => p.Symbol == symbolToTrade);

            if (sameSymbolOpenPositions.Any())
            {
                return false;
            }

            var referenceStopLossRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:StopLossRoi");

            if (candlestick.Count < lookbackPeriods)
            {
                return false;
            }

            var lookbackCandlestick = candlestick[^lookbackPeriods..];
            var last24HoursLowestPriceCandle = lookbackCandlestick.MinBy(candle => candle.LowPrice);
            var last24HoursHighestPriceCandle = lookbackCandlestick.MaxBy(candle => candle.HighPrice);
            var last24HoursLowestPriceIndex = lookbackCandlestick.IndexOf(last24HoursLowestPriceCandle);
            var last24HoursHighestPriceIndex = lookbackCandlestick.IndexOf(last24HoursHighestPriceCandle);
            var expectedStopLossPrice = newPositionSide == OrderSide.Buy
                ? lookbackCandlestick[last24HoursHighestPriceIndex..].Min(candle => candle.LowPrice)
                : lookbackCandlestick[last24HoursLowestPriceIndex..].Max(candle => candle.HighPrice);
            var entryPrice = trade.EntryPrice;
            var expectedStopLossRoi = Math.Abs((expectedStopLossPrice / entryPrice) - 1) * 100 * leverage;
            var maximumTrailingStopRoi = 10 * leverage;
            var actualStopLossRoi = new[] { expectedStopLossRoi, referenceStopLossRoi, maximumTrailingStopRoi }.Min();
            var marginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var availableBalance = Math.Max(account.AvailableBalance, 40);
            var stopLossRatio = referenceStopLossRoi / actualStopLossRoi;
            var margin = stopLossRatio * marginPercentage * availableBalance / 100;
            var notional = margin * leverage;
            var expectedQuantity = notional / entryPrice;

            var minNotionalFilter = information?.MinNotionalFilter;
            var marketLotSizeFilter = information?.MarketLotSizeFilter;
            var quantity = _binance.ApplyMarketQuantityFilter(expectedQuantity,
                entryPrice,
                minNotionalFilter,
                marketLotSizeFilter);

            if (quantity > expectedQuantity)
            {
                _binance.TryReduceMarketQuantity(quantity,
                    entryPrice,
                    minNotionalFilter,
                    marketLotSizeFilter,
                    out var reducedQuantity);

                var quantityFactor = expectedQuantity / reducedQuantity;

                actualStopLossRoi *= quantityFactor;
            }

            //if (actualStopLossRoi < expectedStopLossRoi)
            //{
            //    _logger.LogInformation(
            //        "Expected Stop Loss ROI: {ExpectedStopLossRoi:F0}{NewLine1}" +
            //        "Actual Stop Loss ROI: {ActualStopLossRoi}",
            //        expectedStopLossRoi, Environment.NewLine,
            //        actualStopLossRoi);

            //    return false;
            //}

            actualStopLossRoi = new[] { expectedStopLossRoi.Round(), referenceStopLossRoi, maximumTrailingStopRoi }.Min();

            if (symbolToTrade[withoutQuoteAsset] is not "BTC")
            {
                foreach (var position in openPositions)
                {
                    var otherCandlestick = GetCandlestick(lastCandleId with { Symbol = position.Symbol }, lookbackPeriods);

                    if (candlestick.IsCorrelatedWith(otherCandlestick))
                    {
                        return false;
                    }
                }

                //var minimumProfit = notional / leverage * maxStopLossRoi / 100;

                //if (todayOpenPositions.Any(p => p.UnrealizedPnl < minimumProfit))
                //{
                //    return false;
                //}
            }

            _logger.LogInformation(
                "Reference Stop Loss ROI: {ReferenceStopLossRoi}{NewLine1}" +
                "Actual Stop Loss ROI: {ActualStopLossRoi}",
                referenceStopLossRoi, Environment.NewLine,
                actualStopLossRoi);

            var isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                entryPrice,
                quantity.WithSide(newPositionSide),
                actualStopLossRoi,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                    entryPrice,
                    quantity.WithSide(newPositionSide),
                    actualStopLossRoi,
                    cancellationToken: cancellationToken);
            }

            if (!isStopLossPlaced)
            {
                return false;
            }

            var isEntryOrderPlaced = await _binance.TryPlaceEntryOrderAsync(symbolToTrade,
                newPositionSide,
                FuturesOrderType.Market,
                quantity,
                entryPrice,
                cancellationToken);

            if (!isEntryOrderPlaced)
            {
                await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                return false;
            }
            if (symbolToTrade[withoutQuoteAsset] is "BTC" && quantity > BitcoinQuantityToReduce)
            {
                var isPositionReduced = await _binance.TryClosePositionAtMarketAsync(symbolToTrade,
                    BitcoinQuantityToReduce.WithSide(newPositionSide),
                    cancellationToken);

                if (!isPositionReduced)
                {
                    isPositionReduced = await _binance.TryClosePositionAtMarketAsync(symbolToTrade,
                        BitcoinQuantityToReduce.WithSide(newPositionSide),
                        cancellationToken);

                    if (!isPositionReduced)
                    {
                        var isPositionClosed = await _binance.TryClosePositionAtMarketAsync(symbolToTrade,
                            quantity.WithSide(newPositionSide),
                            cancellationToken);

                        if (!isPositionClosed)
                        {
                            isPositionClosed = await _binance.TryClosePositionAtMarketAsync(symbolToTrade,
                                quantity.WithSide(newPositionSide),
                                cancellationToken);
                        }

                        if (isPositionClosed)
                        {
                            await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                            return false;
                        }
                    }
                }

                if (isPositionReduced)
                {
                    var remainingQuantity = (quantity - BitcoinQuantityToReduce).WithSide(newPositionSide);

                    await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                    isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                        entryPrice,
                        remainingQuantity,
                        actualStopLossRoi,
                        cancellationToken: cancellationToken);

                    if (!isStopLossPlaced)
                    {
                        await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                        isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                            entryPrice,
                            remainingQuantity,
                            actualStopLossRoi,
                            cancellationToken: cancellationToken);
                    }

                    if (!isStopLossPlaced)
                    {
                        return false;
                    }
                }
            }

            //actualStopLossRoi = new[] { referenceStopLossRoi, maximumTrailingStopRoi }.Min();

            var referenceTrailingStopRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:TrailingStopRoi");

            if (referenceTrailingStopRoi > 0)
            {
                var trailingStopRewardFactor = referenceTrailingStopRoi / referenceStopLossRoi;
                var actualTrailingStopRoi = trailingStopRewardFactor * actualStopLossRoi;
                var callbackRate = actualTrailingStopRoi / leverage;

                var isTrailingStopPlaced = await _binance.TryPlaceTrailingStopAsync(symbolToTrade,
                    newPositionSide.Reverse(),
                    quantity.WithSide(newPositionSide),
                    callbackRate,
                    cancellationToken: cancellationToken);

                if (!isTrailingStopPlaced)
                {
                    await _binance.TryCancelTrailingStopAsync(symbolToTrade, cancellationToken);
                    await _binance.TryPlaceTrailingStopAsync(symbolToTrade,
                        newPositionSide.Reverse(),
                        quantity.WithSide(newPositionSide),
                        callbackRate,
                        cancellationToken: cancellationToken);
                }
            }

            var referenceTakeProfitRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:TakeProfitRoi");

            if (referenceTakeProfitRoi > 0)
            {
                var takeProfitRewardFactor = referenceTakeProfitRoi / referenceStopLossRoi;
                var actualTakeProfitRoi = takeProfitRewardFactor * actualStopLossRoi;

                var isTakeProfitPlaced = await _binance.TryPlaceTakeProfitAsync(symbolToTrade,
                    entryPrice,
                    quantity.WithSide(newPositionSide),
                    actualTakeProfitRoi,
                    includeFees: true,
                    cancellationToken: cancellationToken);

                if (!isTakeProfitPlaced)
                {
                    await _binance.TryCancelTakeProfitAsync(symbolToTrade, cancellationToken);
                    await _binance.TryPlaceTakeProfitAsync(symbolToTrade,
                        entryPrice,
                        quantity.WithSide(newPositionSide),
                        actualTakeProfitRoi,
                        includeFees: true,
                        cancellationToken: cancellationToken);
                }
            }

            return true;
        }

        private List<Candle> GetCandlestick(CandleId lastCandleId, int candlestickSize)
        {
            var symbol = lastCandleId.Symbol;
            var timeFrame = lastCandleId.TimeFrame;
            var candlestickId = new CandlestickId(symbol, timeFrame);
            var candlestick = new CircularTimeSeries<CandlestickId, Candle>(candlestickId, candlestickSize);
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                var timeFrameInSeconds = (long)timeFrame;
                var lastCandleOpenTime = lastCandleId.OpenTime;
                var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(lastCandleOpenTime) / timeFrameInSeconds;
                var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * timeFrameInSeconds);
                var remainingCandles = candlestickSize - 1;

                Enumerable.Repeat(lastCandleOpenTime, candlestickSize)
                    .Select(openTime => lastCandleTime.AddSeconds(-(timeFrameInSeconds * remainingCandles--)))
                    .Select(openTime => new CandleId(symbol, timeFrame, openTime))
                    .ToList()
                    .ForEach(candleId =>
                    {
                        var candle = default(Candle);
                        var status = session.Read(ref candleId, ref candle);

                        if (status.Found)
                        {
                            candlestick.Add(candleId.OpenTime, candle);
                        }
                    });
            }

            return candlestick.Snapshot();
        }

        private static bool IsPositionOpen(BinancePositionInfoUsdt position)
        {
            return position is { EntryPrice: > 0, Quantity: > 0 or < 0 };
        }


        private static bool IsPositionUpdatedToday(BinancePositionInfoUsdt position)
        {
            return position.UpdateTime is { Date: { } date } && date == DateTime.UtcNow.Date;
        }

        private static bool IsBtcPosition(BinancePositionInfoUsdt position)
        {
            return position is { Symbol: "BTCUSDT" or "BTCUSDC" };
        }
    }
}

