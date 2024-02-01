using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200ClosePositionTracker : IObserver<CircularTimeSeries<string, IBinanceKline>>
    {
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
            var rsi200 = candles.Select(ToQuote).GetRsi(Length.TwoHundred).ToArray();
            var penultimateRsi200 = rsi200[^2].Rsi!.Value;
            var lastRsi200 = rsi200[^1].Rsi!.Value;
            var isBecomingOversold = penultimateRsi200 >= Rsi.Oversold && lastRsi200 < Rsi.Oversold;
            var isBecomingOverbought = penultimateRsi200 < Rsi.Overbought && lastRsi200 >= Rsi.Overbought;
            var isCrossingMiddleRsi = (penultimateRsi200 < Rsi.Middle && lastRsi200 >= Rsi.Middle)
                || (penultimateRsi200 > Rsi.Middle && lastRsi200 <= Rsi.Middle);

            return isBecomingOversold
                || isCrossingMiddleRsi
                || isBecomingOverbought;
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
