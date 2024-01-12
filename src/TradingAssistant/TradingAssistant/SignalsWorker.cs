﻿using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Converters;
using MediatR;
using Skender.Stock.Indicators;

namespace TradingAssistant
{
    public class SignalsWorker : BackgroundService
    {
        private const int Length8 = 8;
        private const int Length20 = 20;
        private const int Length200 = 200;
        private readonly ILogger<SignalsWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;
        private readonly IPublisher _publisher;
        private readonly KlineInterval _timeFrame;
        private readonly int _lengthA;
        private readonly int _lengthB;
        private readonly int _lengthC;
        private readonly int _lengthD;
        private readonly decimal _minRoiToLengthA;
        private readonly decimal _minRoiToLengthB;
        private readonly decimal _minRoiToLengthC;
        private readonly decimal _minRoiToLengthD;

        public SignalsWorker(ILogger<SignalsWorker> logger,
            IConfiguration configuration,
            BinanceService binanceService,
            IPublisher publisher)
        {
            _logger = logger;
            _configuration = configuration;
            _binanceService = binanceService;
            _publisher = publisher;

            _timeFrame = _configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _lengthA = _configuration.GetValue<int>("Binance:Strategy:LengthA");
            _lengthB = _configuration.GetValue<int>("Binance:Strategy:LengthB");
            _lengthC = _configuration.GetValue<int>("Binance:Strategy:LengthC");
            _lengthD = _configuration.GetValue<int>("Binance:Strategy:LengthD");
            _minRoiToLengthA = _configuration.GetValue<decimal>("Binance:Strategy:MinRoiToLengthA");
            _minRoiToLengthB = _configuration.GetValue<decimal>("Binance:Strategy:MinRoiToLengthB");
            _minRoiToLengthC = _configuration.GetValue<decimal>("Binance:Strategy:MinRoiToLengthC");
            _minRoiToLengthD = _configuration.GetValue<decimal>("Binance:Strategy:MinRoiToLengthD");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binanceService.SubscribeToCandleClosedUpdates((symbol, candlestick) =>
            {
                var lookbackPeriod = _lengthD + 1;
                var candles = candlestick.Last(lookbackPeriod);

                if (candles.Count < lookbackPeriod)
                {
                    return;
                }

                if (!_binanceService.TryGetLeverage(symbol, out var leverage))
                {
                    return;
                }

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
                        EnumConverter.GetString(_timeFrame),
                        Environment.NewLine);

                    _publisher.Publish(new EmaReversionSignal
                    {
                        Time = time,
                        TimeFrame = _timeFrame,
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
                        EnumConverter.GetString(_timeFrame),
                        Environment.NewLine);

                    _publisher.Publish(new EmaReversionSignal
                    {
                        Time = time,
                        TimeFrame = _timeFrame,
                        Direction = PositionSide.Short,
                        Side = OrderSide.Sell,
                        Symbol = symbol,
                        EntryPrice = entryPrice,
                    });

                    Console.Beep(frequency: 1000, duration: 250);
                    Console.Beep(frequency: 1000, duration: 250);
                }
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private bool IsLongPattern(List<IBinanceKline> candles, int leverage)
        {
            var lastCandle = candles.Last();

            if (lastCandle.ClosePrice <= lastCandle.OpenPrice)
            {
                return false;
            }

            var quotes = candles.Select(ToQuote);

            const int OversoldRsi = 30;

            var penultimateRsi8 = (decimal)quotes.GetRsi(Length8).TakeLast(2).First().Rsi!.Value;
            var penultimateRsi20 = (decimal)quotes.GetRsi(Length20).TakeLast(2).First().Rsi!.Value;

            if (penultimateRsi8 > OversoldRsi || penultimateRsi20 > OversoldRsi)
            {
                return false;
            }

            var lastRsi8 = (decimal)quotes.GetRsi(Length8).Last().Rsi!.Value;
            var lastRsi20 = (decimal)quotes.GetRsi(Length20).Last().Rsi!.Value;

            if (lastRsi8 <= OversoldRsi && lastRsi20 <= OversoldRsi)
            {
                return false;
            }

            var rsi200 = (decimal)quotes.GetRsi(Length200).Last().Rsi!.Value;

            if (rsi200 >= 45)
            {
                return false;
            }

            if (lastRsi20 >= lastRsi8)
            {
                return false;
            }

            var emaA = (decimal)quotes.GetEma(_lengthA).Last().Ema!.Value;
            var emaB = (decimal)quotes.GetEma(_lengthB).Last().Ema!.Value;

            if (emaA <= emaB)
            {
                return false;
            }
            var emaC = (decimal)quotes.GetEma(_lengthC).Last().Ema!.Value;

            if (emaB <= emaC)
            {
                return false;
            }

            var emaD = (decimal)quotes.GetEma(_lengthD).Last().Ema!.Value;

            if (emaC <= emaD)
            {
                return false;
            }

            var distanceToEmaA = Math.Abs(((emaA / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaA = _minRoiToLengthA / leverage;

            if (distanceToEmaA <= minDistanceToEmaA)
            {
                return false;
            }

            var distanceToEmaB = Math.Abs(((emaB / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaB = _minRoiToLengthB / leverage;

            if (distanceToEmaB <= minDistanceToEmaB)
            {
                return false;
            }

            var distanceToEmaC = Math.Abs(((emaC / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaC = _minRoiToLengthC / leverage;

            if (distanceToEmaC <= minDistanceToEmaC)
            {
                return false;
            }

            var distanceToEmaD = Math.Abs(((emaD / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaD = _minRoiToLengthD;

            return distanceToEmaD > minDistanceToEmaD;
        }

        private bool IsShortPattern(List<IBinanceKline> candles, int leverage)
        {
            var lastCandle = candles.Last();

            if (lastCandle.ClosePrice >= lastCandle.OpenPrice)
            {
                return false;
            }

            var quotes = candles.Select(ToQuote);

            const int OverboughtRsi = 70;

            var penultimateRsi8 = (decimal)quotes.GetRsi(Length8).TakeLast(2).First().Rsi!.Value;
            var penultimateRsi20 = (decimal)quotes.GetRsi(Length20).TakeLast(2).First().Rsi!.Value;

            if (penultimateRsi8 < OverboughtRsi || penultimateRsi20 < OverboughtRsi)
            {
                return false;
            }

            var lastRsi8 = (decimal)quotes.GetRsi(Length8).Last().Rsi!.Value;
            var lastRsi20 = (decimal)quotes.GetRsi(Length20).Last().Rsi!.Value;

            if (lastRsi8 >= OverboughtRsi && lastRsi20 >= OverboughtRsi)
            {
                return false;
            }

            var rsi200 = (decimal)quotes.GetRsi(Length200).Last().Rsi!.Value;

            if (rsi200 <= 55)
            {
                return false;
            }

            if (lastRsi8 >= lastRsi20)
            {
                return false;
            }

            var emaA = (decimal)quotes.GetEma(_lengthA).Last().Ema!.Value;
            var emaB = (decimal)quotes.GetEma(_lengthB).Last().Ema!.Value;

            if (emaA <= emaB)
            {
                return false;
            }

            var emaC = (decimal)quotes.GetEma(_lengthC).Last().Ema!.Value;

            if (emaB <= emaC)
            {
                return false;
            }

            var emaD = (decimal)quotes.GetEma(_lengthD).Last().Ema!.Value;

            if (emaC <= emaD)
            {
                return false;
            }

            var distanceToEmaA = Math.Abs(((emaA / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaA = _minRoiToLengthA / leverage;

            if (distanceToEmaA <= minDistanceToEmaA)
            {
                return false;
            }

            var distanceToEmaB = Math.Abs(((emaB / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaB = _minRoiToLengthB / leverage;

            if (distanceToEmaB <= minDistanceToEmaB)
            {
                return false;
            }

            var distanceToEmaC = Math.Abs(((emaC / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaC = _minRoiToLengthC / leverage;

            if (distanceToEmaC <= minDistanceToEmaC)
            {
                return false;
            }

            var distanceToEmaD = Math.Abs(((emaD / lastCandle.ClosePrice) - 1) * 100);
            var minDistanceToEmaD = _minRoiToLengthD;

            return distanceToEmaD > minDistanceToEmaD;
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
