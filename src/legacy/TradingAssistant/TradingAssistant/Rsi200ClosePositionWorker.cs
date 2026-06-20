using System.Collections.Concurrent;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using FASTER.core;

namespace TradingAssistant
{
    public class Rsi200ClosePositionWorker : BackgroundService
    {
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly BinanceService _service;
        private readonly KlineInterval _interval;
        private readonly int _candlestickSize;
        private readonly ConcurrentDictionary<string, Rsi200ClosePositionTracker> _closePositionTrackers = [];

        public Rsi200ClosePositionWorker(IConfiguration configuration,
            FasterKV<CandleId, Candle> cache,
            BinanceService binanceService)
        {
            _cache = cache;
            _service = binanceService;

            _interval = configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
            _candlestickSize = configuration.GetValue<int>("Binance:Service:CandlestickSize");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _service.SubscribeToAccountUpdates(HandleAccountUpdate);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                //if (position.EntryPrice != 0 && position.Quantity != 0)
                //{
                //    var candleClosedEvent = _service.GetCandleClosedEvent();

                //    if (_closePositionTrackers.TryRemove(position.Symbol, out var closePositionTracker))
                //    {
                //        closePositionTracker.Unsubscribe();
                //    }

                //    closePositionTracker = new Rsi200ClosePositionTracker(_cache,
                //        _interval,
                //        _candlestickSize,
                //        position,
                //        _service);

                //    closePositionTracker.SubscribeTo(candleClosedEvent);

                //    _closePositionTrackers.TryAdd(position.Symbol, closePositionTracker);
                //}
                //else
                //{
                //    if (_closePositionTrackers.TryRemove(position.Symbol, out var takeProfit))
                //    {
                //        takeProfit.Unsubscribe();
                //    }
                //}
            }
        }
    }
}
