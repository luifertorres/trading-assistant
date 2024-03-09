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

        private bool IsRsi200GettingOutOfLimits(List<IBinanceKline> candles)
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
                && (lastRsi200 > Rsi.Overbought)
                && (penultimateRsi200 < lastRsi200);

            return _position.Quantity.AsOrderSide() switch
            {
                //OrderSide.Buy => isOverbought || isLosingLong,

                //OrderSide.Sell => isOversold || isLosingShort,

                //_ => false,

                _ => isOverbought || isOversold,
            };
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
