using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200ClosePositionTracker : IObserver<CircularTimeSeries<string, IBinanceKline>>
    {
        private const int Length200 = 200;
        private const int OverboughtRsi = 55;
        private const int MiddleRsi = 50;
        private const int OversoldRsi = 45;
        private readonly BinanceFuturesStreamPosition _position;
        private readonly BinanceService _service;
        private IDisposable? _unsubscriber;

        public Rsi200ClosePositionTracker(BinanceFuturesStreamPosition position, BinanceService service)
        {
            _position = position;
            _service = service;
        }

        public virtual void SubscribeTo(IObservable<CircularTimeSeries<string, IBinanceKline>> provider)
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

        public virtual async void OnNext(CircularTimeSeries<string, IBinanceKline> candlestick)
        {
            if (candlestick.Symbol != _position.Symbol)
            {
                return;
            }

            var candles = candlestick.Snapshot();

            if (IsRsi200GettingOutOfLimits(candles))
            {
                await _service.TryClosePositionAtMarketAsync(_position.Symbol, _position.Quantity);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private static bool IsRsi200GettingOutOfLimits(List<IBinanceKline> candles)
        {
            var rsi200 = candles.Select(ToQuote).GetRsi(Length200).ToArray();
            var penultimateRsi200 = (decimal)rsi200[^2].Rsi!.Value;
            var lastRsi200 = (decimal)rsi200[^1].Rsi!.Value;
            var isBecomingOversold = penultimateRsi200 >= OversoldRsi && lastRsi200 < OversoldRsi;
            var isBecomingOverbought = penultimateRsi200 < OverboughtRsi && lastRsi200 >= OverboughtRsi;
            var isCrossingMiddleRsi = (penultimateRsi200 < MiddleRsi && lastRsi200 >= MiddleRsi)
                || (penultimateRsi200 > MiddleRsi && lastRsi200 <= MiddleRsi);

            return isBecomingOversold || isCrossingMiddleRsi || isBecomingOverbought;
        }

        private static Quote ToQuote(IBinanceKline candle)
        {
            return new Quote
            {
                Date = candle.OpenTime,
                Open = candle.OpenPrice,
                High = candle.HighPrice,
                Low = candle.LowPrice,
                Close = candle.ClosePrice,
                Volume = candle.Volume,
            };
        }
    }
}
