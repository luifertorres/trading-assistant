using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Converters.SystemTextJson;
using FASTER.core;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200ClosePositionTracker : IObserver<CandleId>
    {
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly KlineInterval _interval;
        private readonly int _candlestickSize;
        private readonly BinanceFuturesStreamPosition _position;
        private readonly BinanceService _service;
        private IDisposable? _unsubscriber;

        public Rsi200ClosePositionTracker(FasterKV<CandleId, Candle> cache,
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

            if (IsRsi200GettingOutOfLimits([.. candlestick.Values]))
            {
                await _service.TryClosePositionAtMarketAsync(_position.Symbol, _position.Quantity);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private bool IsRsi200GettingOutOfLimits(List<Candle> candles)
        {
            var rsi200 = candles.Select(ToQuote).GetRsi(Length.TwoHundred).ToArray();
            var penultimateRsi200 = rsi200[^2].Rsi!.Value;
            var lastRsi200 = rsi200[^1].Rsi!.Value;
            var isOversold = penultimateRsi200 >= Rsi.OversoldFor200 && lastRsi200 < Rsi.OversoldFor200;
            var isOverbought = penultimateRsi200 < Rsi.OverboughtFor200 && lastRsi200 >= Rsi.OverboughtFor200;
            var isCrossingMiddleRsi = (penultimateRsi200 < Rsi.Middle && lastRsi200 >= Rsi.Middle)
                || (penultimateRsi200 > Rsi.Middle && lastRsi200 <= Rsi.Middle);
            var lastPrice = candles.Last().ClosePrice;
            var isLosingLong = lastPrice < _position.EntryPrice
                && (lastRsi200 < Rsi.Oversold)
                && (penultimateRsi200 > lastRsi200);
            var isLosingShort = lastPrice > _position.EntryPrice
                && (penultimateRsi200 < lastRsi200);

            return _position.Quantity.AsOrderSide() switch
            {
                //OrderSide.Buy => isOverbought || isLosingLong,

                //OrderSide.Sell => isOversold || isLosingShort,

                //_ => false,

                _ => isOverbought || isOversold,
            };
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
