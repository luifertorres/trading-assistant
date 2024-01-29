using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Converters;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class Rsi200SignalGenerator : IObserver<CircularTimeSeries<string, IBinanceKline>>
    {
        private const int Length200 = 200;
        private const int OversoldRsi = 45;
        private const int OverboughtRsi = 55;
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

        public virtual void OnNext(CircularTimeSeries<string, IBinanceKline> candlestick)
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

                _publisher.Publish(new EmaReversionSignal
                {
                    Time = time,
                    TimeFrame = _timeFrame,
                    Direction = positionSide,
                    Side = signalOrderSide.Value,
                    Symbol = candlestick.Symbol,
                    EntryPrice = entryPrice,
                });

                Console.Beep(frequency: 500, duration: 500);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        private static OrderSide? GetSignalOrderSide(List<IBinanceKline> candles)
        {
            var rsi200 = candles.Select(ToQuote).GetRsi(Length200).ToArray();
            var penultimateRsi200 = (decimal)rsi200[^2].Rsi!.Value;
            var lastRsi200 = (decimal)rsi200[^1].Rsi!.Value;

            return (penultimateRsi200, lastRsi200) switch
            {
                ( <= OversoldRsi, > OversoldRsi) => OrderSide.Buy,
                ( >= OverboughtRsi, < OverboughtRsi) => OrderSide.Sell,
                _ => null,
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
