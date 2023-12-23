using Binance.Net.Interfaces;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class SignalsWorker(ILogger<SignalsWorker> logger, BinanceService binanceService) : BackgroundService
    {
        private const int Period5 = 5;
        private const int Period8 = 8;
        private const int Period20 = 20;
        private const int Period200 = 200;
        private const int Period60 = 60;
        private const int Period96 = 96;
        private const int Period240 = 240;
        private readonly ILogger<SignalsWorker> _logger = logger;
        private readonly BinanceService _binanceService = binanceService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binanceService.SubscribeToCandleClosedUpdates((symbol, candlestick) =>
            {
                var slowestPeriod = Period240;
                var candles = candlestick.Last(slowestPeriod);

                if (candles.Count < slowestPeriod)
                {
                    return;
                }

                if (!_binanceService.TryGetLeverage(symbol, out var leverage))
                {
                    return;
                }

                if (HasPatternMatching(candles, leverage))
                {
                    _logger.LogInformation("{Time}, 5m, SHORT in {Symbol} @ {Price}",
                        candles.Last().CloseTime.AddSeconds(1).ToLocalTime().ToString("HH:mm"),
                        symbol,
                        candles.TakeLast(2).First().ClosePrice);

                    Console.Beep(frequency: 1000, duration: 100);
                }
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private static bool HasPatternMatching(List<IBinanceKline> candles, int leverage)
        {
            var lastCandle = candles.Last();

            if (lastCandle.ClosePrice >= lastCandle.OpenPrice)
            {
                return false;
            }

            var penultimateCandle = candles.TakeLast(2).First();

            if (penultimateCandle.ClosePrice < penultimateCandle.OpenPrice)
            {
                return false;
            }

            var quotes = candles.Select(ToQuote);
            var rsi20 = (decimal)quotes.GetRsi(Period20).Last().Rsi!.Value;

            if (rsi20 <= 70)
            {
                return false;
            }

            var rsi8 = (decimal)quotes.GetRsi(Period8).Last().Rsi!.Value;

            if (rsi8 <= 70)
            {
                return false;
            }

            var ema5 = (decimal)quotes.GetEma(Period5).Last().Ema!.Value;
            var ema60 = (decimal)quotes.GetEma(Period60).Last().Ema!.Value;

            if (ema5 <= ema60)
            {
                return false;
            }
            var ema96 = (decimal)quotes.GetEma(Period96).Last().Ema!.Value;

            if (ema60 <= ema96)
            {
                return false;
            }

            var ema240 = (decimal)quotes.GetEma(Period240).Last().Ema!.Value;

            if (ema96 <= ema240)
            {
                return false;
            }

            var distanceToEma = Math.Abs(((ema60 / lastCandle.ClosePrice) - 1) * 100);
            var requiredDistance = 300m / leverage;

            //return true;
            return distanceToEma > requiredDistance;
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
