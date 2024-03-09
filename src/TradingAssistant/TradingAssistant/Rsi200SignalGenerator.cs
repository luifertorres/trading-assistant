using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Converters;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200SignalGenerator : IObserver<CircularTimeSeries<string, IBinanceKline>>
    {
        private readonly ILogger<Rsi200SignalGenerator> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPublisher _publisher;
        private readonly KlineInterval _timeFrame;
        private readonly int _candlestickSize;
        private IDisposable? _unsubscriber;

        public Rsi200SignalGenerator(ILogger<Rsi200SignalGenerator> logger,
            IConfiguration configuration,
            IPublisher publisher)
        {
            _logger = logger;
            _configuration = configuration;
            _publisher = publisher;

            _timeFrame = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _candlestickSize = _configuration.GetValue<int>("Binance:Service:CandlestickSize");
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
            var candles = candlestick.Snapshot();

            if (candles.Count < _candlestickSize)
            {
                return;
            }

            var time = candles.Last().CloseTime.AddSeconds(1).ToLocalTime();
            var entryPrice = candles.Last().ClosePrice;
            var signalOrderSide = GetSignalOrderSide(candles);

            if (signalOrderSide is not null)
            {
                var positionSide = signalOrderSide.Value.AsPositionSide();

                _logger.LogInformation("{Time}{NewLine}" +
                    "Binance{NewLine}" +
                    "{Symbol}{NewLine}" +
                    "{PositionSide}{NewLine}" +
                    "@ {Price}{NewLine}" +
                    "{TimeFrame}{NewLine}",
                    time.ToString("HH:mm"),
                    Environment.NewLine,
                    Environment.NewLine,
                    candlestick.Symbol,
                    Environment.NewLine,
                    EnumConverter.GetString(positionSide).ToUpperInvariant(),
                    Environment.NewLine,
                    entryPrice,
                    Environment.NewLine,
                    EnumConverter.GetString(_timeFrame),
                    Environment.NewLine);

                await _publisher.Publish(new EmaReversionSignal
                {
                    Time = time,
                    TimeFrame = _timeFrame,
                    Direction = positionSide,
                    Side = signalOrderSide.Value,
                    Symbol = candlestick.Symbol,
                    EntryPrice = entryPrice,
                });

                //Console.Beep(frequency: 500, duration: 500);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private OrderSide? GetSignalOrderSide(List<IBinanceKline> candles)
        {
            return _timeFrame is KlineInterval.OneMinute
                ? GetRsiSignalsFrom1m(candles)
                : GetRsiSignals(candles);
        }

        private static OrderSide? GetRsiSignalsFrom1m(List<IBinanceKline> candles)
        {
            var lengths = new[]
            {
                Length.Five,
                Length.Eight,
                Length.Twenty,
                Length.TwoHundred
            };

            var timeFrames = new[]
            {
                PeriodSize.OneMinute,
                PeriodSize.FiveMinutes,
                PeriodSize.FifteenMinutes,
                PeriodSize.ThirtyMinutes,
                PeriodSize.OneHour
            };

            foreach (var timeFrame in timeFrames)
            {
                var rsis = lengths.Select(length => timeFrame is PeriodSize.OneMinute
                    ? GetRsi(candles, length)
                    : GetRsi(candles, length, timeFrame));
                var rsi200PenultimateSignal = GetRsi200PenultimateSignal(rsis.Last());
                var areFasterRsiPenultimateSignalsEqualToRsi200Signal = rsis.Take(3)
                    .Select(GetRsiPenultimateSignal)
                    .All(signal => signal == rsi200PenultimateSignal);

                if (!areFasterRsiPenultimateSignalsEqualToRsi200Signal)
                {
                    return null;
                }
            }

            var rsisIn1m = lengths.Select(length => GetRsi(candles, length));
            var rsi200PenultimateSignalIn1m = GetRsi200PenultimateSignal(rsisIn1m.Last());
            var isAnyFasterRsiLastSignalEqualToRsi200SignalIn1m = rsisIn1m.Take(3)
                .Select(GetRsiLastSignal)
                .Any(signal => signal == rsi200PenultimateSignalIn1m);

            if (!isAnyFasterRsiLastSignalEqualToRsi200SignalIn1m)
            {
                return null;
            }

            return rsi200PenultimateSignalIn1m;

            static OrderSide? GetRsi200PenultimateSignal(double[] rsi) => rsi switch
            {
                [.., <= Rsi.OversoldFor200, _] => OrderSide.Buy,
                [.., >= Rsi.OverboughtFor200, _] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            static OrderSide? GetRsiPenultimateSignal(double[] rsi) => rsi switch
            {
                [.., <= Rsi.Oversold, _] => OrderSide.Buy,
                [.., >= Rsi.Overbought, _] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            static OrderSide? GetRsiLastSignal(double[] rsi) => rsi switch
            {
                [.., <= Rsi.Oversold, > Rsi.Oversold] => OrderSide.Buy,
                [.., >= Rsi.Overbought, < Rsi.Overbought] => OrderSide.Sell,

                _ => default(OrderSide?),
            };
        }

        private static OrderSide? GetRsiSignals(List<IBinanceKline> candles)
        {
            var lengths = new[] { Length.Five, Length.Eight, Length.Twenty, Length.TwoHundred };
            var rsis = lengths.Select(length => GetRsi(candles, length));
            var rsi200Signal = rsis.Last() switch
            {
                [.., <= Rsi.OversoldFor200, _] => OrderSide.Buy,
                [.., >= Rsi.OverboughtFor200, _] => OrderSide.Sell,

                _ => default(OrderSide?),
            };

            var areFasterRsiSignalsEqualToRsi200Signal = rsis.Take(3)
                .Select(rsi => rsi switch
                {
                    [.., <= Rsi.Oversold, > Rsi.Oversold] => OrderSide.Buy,
                    [.., >= Rsi.Overbought, < Rsi.Overbought] => OrderSide.Sell,

                    _ => default(OrderSide?),
                })
                .All(rsi => rsi == rsi200Signal);

            return areFasterRsiSignalsEqualToRsi200Signal ? rsi200Signal : null;
        }

        private static double[] GetRsi(List<IBinanceKline> candles, int length, PeriodSize? higherTimeFrame = null)
        {
            var quotes = candles.Select(ToQuote).Validate();

            var postWarmupPeriod = 102;

            if (higherTimeFrame.HasValue)
            {
                return quotes.Aggregate(higherTimeFrame.Value)
                    .TakeLast(length + postWarmupPeriod)
                    .GetRsi(length)
                    .Select(rsi => rsi.Rsi.GetValueOrDefault())
                    .ToArray();
            }

            return quotes.TakeLast(length + postWarmupPeriod)
                .GetRsi(length)
                .Select(rsi => rsi.Rsi.GetValueOrDefault())
                .ToArray();
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
