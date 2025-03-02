using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Ema5ClosePositionTracker : IObserver<CandleId>
    {
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly KlineInterval _interval;
        private readonly int _candlestickSize;
        private readonly BinanceFuturesStreamPosition _position;
        private readonly BinanceService _service;
        private IDisposable? _unsubscriber;

        public Ema5ClosePositionTracker(FasterKV<CandleId, Candle> cache,
            KlineInterval interval,
            int candlestickSize,
            BinanceFuturesStreamPosition position,
            BinanceService service)
        {
            _cache = cache;
            _interval = interval;
            _candlestickSize = candlestickSize;
            _position = position;
            _service = service;
        }

        public virtual void SubscribeTo(IObservable<CandleId> provider)
        {
            if (provider is not null)
            {
                _unsubscriber = provider.Subscribe(this);
            }
        }

        public virtual void OnCompleted()
        {
            Unsubscribe();
        }

        public virtual void OnError(Exception exception)
        {
        }

        public virtual async void OnNext(CandleId lastCandleId)
        {
            var candlestick = new SortedList<DateTime, Candle>();
            var sessionBuilder = _cache.For(new SimpleFunctions<CandleId, Candle>());

            using (var session = sessionBuilder.NewSession<SimpleFunctions<CandleId, Candle>>())
            {
                var lastCandleOpenTimeTotalSeconds = DateTimeConverter.ConvertToSeconds(lastCandleId.OpenTime) / (long)_interval;
                var lastCandleTime = DateTimeConverter.ConvertFromSeconds((double)lastCandleOpenTimeTotalSeconds * (long)_interval);
                var idsLeft = _candlestickSize - 1;

                Enumerable.Repeat(lastCandleId.OpenTime, _candlestickSize)
                    .Select(openTime => lastCandleTime.AddSeconds(-((int)_interval * idsLeft--)))
                    .Select(openTime => new CandleId(lastCandleId.Symbol, _interval, openTime))
                    .ToList()
                    .ForEach(candleId =>
                    {
                        var candle = default(Candle);

                        session.Read(ref candleId, ref candle);

                        if (candleId.OpenTime == candle.OpenTime)
                        {
                            candlestick.Add(candleId.OpenTime, candle);
                        }
                    });
            }

            if (IsPriceCrossingEma5([.. candlestick.Values], _position.Quantity.AsOrderSide()))
            {
                await _service.TryClosePositionAtMarketAsync(_position.Symbol, _position.Quantity);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private bool IsPriceCrossingEma5(List<Candle> candles, OrderSide positionSide)
        {
            var ema5 = candles.Select(ToQuote)
                .GetEma(5)
                .Last().Ema!.Value;

            return positionSide is OrderSide.Buy
                ? candles.Last().ClosePrice >= (decimal)ema5
                : candles.Last().ClosePrice <= (decimal)ema5;
        }

        private static Quote ToQuote(Candle candle)
        {
            return new Quote
            {
                Date = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
            };
        }
    }
}
