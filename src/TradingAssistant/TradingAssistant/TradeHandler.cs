using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using MediatR;

namespace TradingAssistant
{
    public class TradeHandler : IRequestHandler<TradeRequest, bool>
    {
        private const string Usdt = "USDT";
        private const string Usdc = "USDC";
        private readonly ILogger<TradeHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly BinanceService _binance;
        private readonly KlineInterval _timeFrame;
        private readonly int _candlestickSize;

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
            _candlestickSize = _configuration.GetValue<int>("Binance:Service:CandlestickSize");
        }

        public async Task<bool> Handle(TradeRequest trade, CancellationToken cancellationToken)
        {
            var symbolToTrade = trade.Symbol;

            if (!_binance.TryGetSymbolInformation(symbolToTrade, out var symbolToTradeInformation))
            {
                return false;
            }

            if (!_binance.TryGetLeverage(symbolToTrade, out var leverage))
            {
                return false;
            }

            var lastCandleId = new CandleId(symbolToTrade, _timeFrame, trade.Time);
            var candlestick = GetCandlestick(lastCandleId);
            var bitcoin = GetCandlestick(lastCandleId with { Symbol = "BTCUSDT" });
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
            var lookbackPeriods = 10; // Math.Max(60 * 60 * 24 / (int)trade.TimeFrame, 1);
            var desiredStopLossPrice = newPositionSide == OrderSide.Buy
                ? candlestick[^lookbackPeriods..].Min(candle => candle.LowPrice)
                : candlestick[^lookbackPeriods..].Max(candle => candle.HighPrice);
            var entryPrice = trade.EntryPrice;
            var desiredStopLossRoi = Math.Abs((desiredStopLossPrice / entryPrice) - 1) * 100 * leverage;
            var maximumTrailingStopRoi = 10 * leverage;
            var actualStopLossRoi = Math.Min(desiredStopLossRoi, maximumTrailingStopRoi);
            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var availableBalance = Math.Max(account.AvailableBalance, 40);
            var stopLossBasedFactor = referenceStopLossRoi / actualStopLossRoi;
            var desiredMargin = stopLossBasedFactor * accountMarginPercentage * availableBalance / 100;
            var desiredNotional = desiredMargin * leverage;
            var desiredPositionQuantity = desiredNotional / entryPrice;
            var newPositionQuantity = _binance.ApplyMarketQuantityFilter(desiredPositionQuantity,
                entryPrice,
                symbolToTradeInformation?.MinNotionalFilter,
                symbolToTradeInformation?.MarketLotSizeFilter);

            if (newPositionQuantity > desiredPositionQuantity)
            {
                var newPositionQuantityBasedFactor = desiredPositionQuantity / newPositionQuantity;

                actualStopLossRoi *= newPositionQuantityBasedFactor;
            }

            if (symbolToTrade[withoutQuoteAsset] is "BTC" && (newPositionQuantity > 0.001m))
            {
                const decimal BitcoinQuantityToReduce = 0.001m;

                var newPositionQuantityBasedFactor = newPositionQuantity / (newPositionQuantity - BitcoinQuantityToReduce);

                newPositionQuantity -= BitcoinQuantityToReduce;
                actualStopLossRoi *= newPositionQuantityBasedFactor;
            }

            actualStopLossRoi = Math.Min(desiredStopLossRoi, maximumTrailingStopRoi);

            if (symbolToTrade[withoutQuoteAsset] is not "BTC")
            {
                foreach (var position in openPositions)
                {
                    var otherCandlestick = GetCandlestick(lastCandleId with { Symbol = position.Symbol });

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
                "Reference Stop Loss ROI: {ReferenceStopLossRoi:F0}{NewLine1}" +
                "Actual Stop Loss ROI: {ActualStopLossRoi:F0}",
                referenceStopLossRoi, Environment.NewLine,
                actualStopLossRoi);

            var isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                entryPrice,
                newPositionQuantity.WithSide(newPositionSide),
                actualStopLossRoi,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                isStopLossPlaced = await _binance.TryPlaceStopLossAsync(symbolToTrade,
                    entryPrice,
                    newPositionQuantity.WithSide(newPositionSide),
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
                newPositionQuantity,
                entryPrice,
                cancellationToken);

            if (!isEntryOrderPlaced)
            {
                await _binance.TryCancelStopLossAsync(symbolToTrade, cancellationToken);

                return false;
            }

            var referenceTrailingStopRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:TrailingStopRoi");

            if (referenceTrailingStopRoi > 0)
            {
                var trailingStopRewardFactor = referenceTrailingStopRoi / referenceStopLossRoi;
                var actualTrailingStopRoi = trailingStopRewardFactor * actualStopLossRoi;
                var callbackRate = actualTrailingStopRoi / leverage;

                var isTrailingStopPlaced = await _binance.TryPlaceTrailingStopAsync(symbolToTrade,
                    newPositionSide.Reverse(),
                    newPositionQuantity.WithSide(newPositionSide),
                    callbackRate,
                    cancellationToken: cancellationToken);

                if (!isTrailingStopPlaced)
                {
                    await _binance.TryCancelTrailingStopAsync(symbolToTrade, cancellationToken);
                    await _binance.TryPlaceTrailingStopAsync(symbolToTrade,
                        newPositionSide.Reverse(),
                        newPositionQuantity.WithSide(newPositionSide),
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
                    newPositionQuantity.WithSide(newPositionSide),
                    actualTakeProfitRoi,
                    includeFees: true,
                    cancellationToken: cancellationToken);

                if (!isTakeProfitPlaced)
                {
                    await _binance.TryCancelTakeProfitAsync(symbolToTrade, cancellationToken);
                    await _binance.TryPlaceTakeProfitAsync(symbolToTrade,
                        entryPrice,
                        newPositionQuantity.WithSide(newPositionSide),
                        actualTakeProfitRoi,
                        includeFees: true,
                        cancellationToken: cancellationToken);
                }
            }

            return true;
        }

        private List<Candle> GetCandlestick(CandleId lastCandleId)
        {
            var symbol = lastCandleId.Symbol;
            var timeFrame = lastCandleId.TimeFrame;
            var candlestickId = new CandlestickId(symbol, timeFrame);
            var candlestick = new CircularTimeSeries<CandlestickId, Candle>(candlestickId, _candlestickSize);
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                var timeFrameInSeconds = (long)timeFrame;
                var lastCandleOpenTime = lastCandleId.OpenTime;
                var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(lastCandleOpenTime) / timeFrameInSeconds;
                var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * timeFrameInSeconds);
                var remainingCandles = _candlestickSize - 1;

                Enumerable.Repeat(lastCandleOpenTime, _candlestickSize)
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
