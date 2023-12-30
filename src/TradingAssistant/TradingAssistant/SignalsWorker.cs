using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Converters;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class SignalsWorker : BackgroundService
    {
        private const int Length5 = 5;
        private const int Length8 = 8;
        private const int Length20 = 20;
        private const int Length200 = 200;
        private const int Length60 = 60;
        private const int Length96 = 96;
        private const int Length240 = 240;
        private const int LengthA = Length5;
        private const int LengthB = Length60;
        private const int LengthC = Length96;
        private const int LengthD = Length240;
        private readonly ILogger<SignalsWorker> _logger;
        private readonly BinanceService _binanceService;
        private readonly IPublisher _publisher;

        public SignalsWorker(ILogger<SignalsWorker> logger, BinanceService binanceService, IPublisher publisher)
        {
            _logger = logger;
            _binanceService = binanceService;
            _publisher = publisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binanceService.SubscribeToCandleClosedUpdates((symbol, candlestick) =>
            {
                var lookbackPeriod = Length240 + 1;
                var candles = candlestick.Last(lookbackPeriod);

                if (candles.Count < lookbackPeriod)
                {
                    return;
                }

                if (!_binanceService.TryGetLeverage(symbol, out var leverage))
                {
                    return;
                }

                const KlineInterval TimeFrame = KlineInterval.FiveMinutes;

                var time = candles.Last().CloseTime.AddSeconds(1).ToLocalTime();
                var entryPrice = candles.Last().ClosePrice;

                if (IsLongPattern(candles, leverage))
                {
                    _logger.LogInformation("{Time}{NewLine}" +
                        "Binance{NewLine}" +
                        "{Symbol}{NewLine}" +
                        "LONG{NewLine}" +
                        "@ {Price}{NewLine}" +
                        "{TimeFrame}{NewLine}",
                        time.ToString("HH:mm"),
                        Environment.NewLine,
                        Environment.NewLine,
                        symbol,
                        Environment.NewLine,
                        Environment.NewLine,
                        entryPrice,
                        Environment.NewLine,
                        EnumConverter.GetString(TimeFrame),
                        Environment.NewLine);

                    _publisher.Publish(new EmaReversionSignal
                    {
                        Time = time,
                        TimeFrame = TimeFrame,
                        Direction = PositionSide.Long,
                        Side = OrderSide.Buy,
                        Symbol = symbol,
                        EntryPrice = entryPrice,
                    });

                    Console.Beep(frequency: 2000, duration: 500);
                }

                if (IsShortPattern(candles, leverage))
                {
                    _logger.LogInformation("{Time}{NewLine}" +
                        "Binance{NewLine}" +
                        "{Symbol}{NewLine}" +
                        "SHORT{NewLine}" +
                        "@ {Price}{NewLine}" +
                        "{TimeFrame}{NewLine}",
                        time.ToString("HH:mm"),
                        Environment.NewLine,
                        Environment.NewLine,
                        symbol,
                        Environment.NewLine,
                        Environment.NewLine,
                        entryPrice,
                        Environment.NewLine,
                        EnumConverter.GetString(TimeFrame),
                        Environment.NewLine);

                    _publisher.Publish(new EmaReversionSignal
                    {
                        Time = time,
                        TimeFrame = TimeFrame,
                        Direction = PositionSide.Short,
                        Side = OrderSide.Sell,
                        Symbol = symbol,
                        EntryPrice = entryPrice,
                    });

                    Console.Beep(frequency: 1000, duration: 100);
                }
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private static bool IsLongPattern(List<IBinanceKline> candles, int leverage)
        {
            var lastCandle = candles.Last();

            if (lastCandle.ClosePrice <= lastCandle.OpenPrice)
            {
                return false;
            }

            var penultimateCandle = candles.TakeLast(2).First();

            if (penultimateCandle.ClosePrice <= penultimateCandle.OpenPrice)
            {
                return false;
            }

            var quotes = candles.Select(ToQuote);
            var rsi20 = (decimal)quotes.GetRsi(Length20).Last().Rsi!.Value;

            const int OversoldRsi20Max = 40;
            const int OversoldRsi20Min = 30;

            if (rsi20 >= OversoldRsi20Max || rsi20 <= OversoldRsi20Min)
            {
                return false;
            }

            const int OversoldRsi8Max = 50;
            const int OversoldRsi8Min = 40;

            var rsi8 = (decimal)quotes.GetRsi(Length8).Last().Rsi!.Value;

            if (rsi8 >= OversoldRsi8Max || rsi8 <= OversoldRsi8Min)
            {
                return false;
            }

            var rsi200 = (decimal)quotes.GetRsi(Length200).Last().Rsi!.Value;

            if (rsi200 >= 45)
            {
                return false;
            }

            if (rsi20 >= rsi8)
            {
                return false;
            }

            var emaA = (decimal)quotes.GetEma(LengthA).Last().Ema!.Value;
            var emaB = (decimal)quotes.GetEma(LengthB).Last().Ema!.Value;

            if (emaA <= emaB)
            {
                return false;
            }
            var emaC = (decimal)quotes.GetEma(LengthC).Last().Ema!.Value;

            if (emaB <= emaC)
            {
                return false;
            }

            var emaD = (decimal)quotes.GetEma(LengthD).Last().Ema!.Value;

            if (emaC <= emaD)
            {
                return false;
            }

            var distanceToFastestEma = Math.Abs(((emaB / lastCandle.ClosePrice) - 1) * 100);
            var distanceToSlowestEma = Math.Abs(((emaD / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToFastestEma = 300m / leverage;
            var minDistanceToSlowestEma = 2 * minDistanceToFastestEma;

            return distanceToFastestEma >= minDistanceToFastestEma && distanceToSlowestEma >= minDistanceToSlowestEma;
        }

        private static bool IsShortPattern(List<IBinanceKline> candles, int leverage)
        {
            var lastCandle = candles.Last();

            if (lastCandle.ClosePrice >= lastCandle.OpenPrice)
            {
                return false;
            }

            var penultimateCandle = candles.TakeLast(2).First();

            if (penultimateCandle.ClosePrice >= penultimateCandle.OpenPrice)
            {
                return false;
            }

            var quotes = candles.Select(ToQuote);
            var rsi20 = (decimal)quotes.GetRsi(Length20).Last().Rsi!.Value;

            const int OverboughtRsi20Max = 70;
            const int OverboughtRsi20Min = 60;

            if (rsi20 <= OverboughtRsi20Min || rsi20 >= OverboughtRsi20Max)
            {
                return false;
            }

            var rsi8 = (decimal)quotes.GetRsi(Length8).Last().Rsi!.Value;

            const int OverboughtRsi8Max = 60;
            const int OverboughtRsi8Min = 50;

            if (rsi8 <= OverboughtRsi8Min || rsi8 >= OverboughtRsi8Max)
            {
                return false;
            }

            var rsi200 = (decimal)quotes.GetRsi(Length200).Last().Rsi!.Value;

            if (rsi200 <= 55)
            {
                return false;
            }

            if (rsi20 <= rsi8)
            {
                return false;
            }

            var emaA = (decimal)quotes.GetEma(LengthA).Last().Ema!.Value;
            var emaB = (decimal)quotes.GetEma(LengthB).Last().Ema!.Value;

            if (emaA <= emaB)
            {
                return false;
            }
            var emaC = (decimal)quotes.GetEma(LengthC).Last().Ema!.Value;

            if (emaB <= emaC)
            {
                return false;
            }

            var emaD = (decimal)quotes.GetEma(LengthD).Last().Ema!.Value;

            if (emaC <= emaD)
            {
                return false;
            }

            var distanceToFastestEma = Math.Abs(((emaB / lastCandle.ClosePrice) - 1) * 100);
            var distanceToSlowestEma = Math.Abs(((emaD / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToFastestEma = 300m / leverage;
            var minDistanceToSlowestEma = 2 * minDistanceToFastestEma;

            return distanceToFastestEma >= minDistanceToFastestEma && distanceToSlowestEma >= minDistanceToSlowestEma;
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
