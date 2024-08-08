using Binance.Net.Enums;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using MediatR;

namespace TradingAssistant
{
    public class TradeHandler : IRequestHandler<TradeRequest, bool>
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly BinanceService _binance;

        public TradeHandler(IConfiguration configuration,
            IServiceScopeFactory factory,
            FasterKV<CandleId, Candle> cache,
            BinanceService binance)
        {
            _configuration = configuration;
            _factory = factory;
            _cache = cache;
            _binance = binance;
        }

        public async Task<bool> Handle(TradeRequest trade, CancellationToken cancellationToken)
        {
            using (var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>())
            {
                if (database.OpenPositions.Any(p => p.Symbol == trade.Symbol))
                {
                    return false;
                }
            }

            if (!_binance.TryGetLeverage(trade.Symbol, out var leverage))
            {
                return false;
            }

            var account = await _binance.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return false;
            }

            var interval = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());
            var candlestickSize = _configuration.GetValue<int>("Binance:Service:CandlestickSize");
            var candlestick = new SortedList<DateTime, Candle>(candlestickSize);
            var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(trade.Time) / (long)interval;
            var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * (long)interval);
            var idsLeft = candlestickSize - 1;

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                Enumerable.Repeat(trade.Time, candlestickSize)
                    .Select(openTime => lastCandleTime.AddSeconds(-((int)interval * idsLeft--)))
                    .Select(openTime => new CandleId(trade.Symbol, interval, openTime))
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

            if (candlestick.Count < candlestickSize)
            {
                return false;
            }

            var candles = candlestick.Select(pair => pair.Value);
            var desiredStopLossRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:StopLossRoi");
            var entryPrice = candles.Last().ClosePrice;
            var actualStopLossRoi = desiredStopLossRoi;

            if (leverage < 50)
            {
                actualStopLossRoi = leverage * 2 * (desiredStopLossRoi / 100);
            }

            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var stopLossBasedMarginFactor = desiredStopLossRoi / actualStopLossRoi;
            var accountPercentageForEntry = stopLossBasedMarginFactor * accountMarginPercentage * leverage;
            var notional = account.AvailableBalance * accountPercentageForEntry / 100;
            var newPositionQuantity = notional / entryPrice;

            var isStopLossPlaced = await _binance.TryPlaceStopLossAsync(trade.Symbol,
                entryPrice,
                newPositionQuantity.WithSide(trade.Side),
                actualStopLossRoi,
                cancellationToken: cancellationToken);

            if (!isStopLossPlaced)
            {
                await _binance.TryCancelStopLossAsync(trade.Symbol, cancellationToken);

                isStopLossPlaced = await _binance.TryPlaceStopLossAsync(trade.Symbol,
                    entryPrice,
                    newPositionQuantity.WithSide(trade.Side),
                    actualStopLossRoi,
                    cancellationToken: cancellationToken);
            }

            if (!isStopLossPlaced)
            {
                return false;
            }

            var isEntryOrderPlaced = await _binance.TryPlaceEntryOrderAsync(trade.Symbol,
                trade.Side,
                FuturesOrderType.Market,
                newPositionQuantity,
                entryPrice,
                cancellationToken);

            if (!isEntryOrderPlaced)
            {
                return false;
            }

            var desiredTakeProfitRoi = _configuration.GetValue<decimal>("Binance:RiskManagement:TakeProfitRoi");
            var rewardFactor = desiredTakeProfitRoi / desiredStopLossRoi;
            var actualTakeProfitRoi = rewardFactor * actualStopLossRoi;

            var isTakeProfitPlaced = await _binance.TryPlaceTakeProfitAsync(trade.Symbol,
                entryPrice,
                newPositionQuantity.WithSide(trade.Side),
                actualTakeProfitRoi,
                cancellationToken: cancellationToken);

            if (!isTakeProfitPlaced)
            {
                await _binance.TryCancelTakeProfitAsync(trade.Symbol, cancellationToken);
                await _binance.TryPlaceTakeProfitAsync(trade.Symbol,
                    entryPrice,
                    newPositionQuantity.WithSide(trade.Side),
                    actualTakeProfitRoi,
                    cancellationToken: cancellationToken);
            }

            var callbackRate = actualStopLossRoi / leverage;

            _binance.TryGetSymbolInformation(trade.Symbol, out var symbolInformation);

            newPositionQuantity = _binance.ApplyMarketQuantityFilter(newPositionQuantity,
                entryPrice,
                symbolInformation?.MinNotionalFilter,
                symbolInformation?.MarketLotSizeFilter);

            var isTrailingStopPlaced = await _binance.TryPlaceTrailingStopAsync(trade.Symbol,
                trade.Side.Reverse(),
                newPositionQuantity.WithSide(trade.Side),
                callbackRate,
                cancellationToken: cancellationToken);

            if (!isTrailingStopPlaced)
            {
                await _binance.TryCancelTrailingStopAsync(trade.Symbol, cancellationToken);
                await _binance.TryPlaceTrailingStopAsync(trade.Symbol,
                    trade.Side.Reverse(),
                    newPositionQuantity.WithSide(trade.Side),
                    callbackRate,
                    cancellationToken: cancellationToken);
            }

            return true;
        }
    }
}
