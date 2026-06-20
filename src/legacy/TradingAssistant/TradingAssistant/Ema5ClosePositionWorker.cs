using System.Collections.Concurrent;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using FASTER.core;

namespace TradingAssistant
{
    public class Ema5ClosePositionWorker : BackgroundService
    {
        private readonly FasterKV<CandleId, Candle> _cache;
        private readonly BinanceService _service;
        private readonly KlineInterval _interval;
        private readonly int _candlestickSize;
        private readonly ConcurrentDictionary<string, Ema5ClosePositionTracker> _closePositionTrackers = [];

        public Ema5ClosePositionWorker(IConfiguration configuration,
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
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    if (_closePositionTrackers.TryRemove(position.Symbol, out var closePositionTracker))
                    {
                        _ = _service.TryUnsubscribeFromPriceAsync(position.Symbol);
                    }

                    closePositionTracker = new Ema5ClosePositionTracker(_cache,
                        _interval,
                        _candlestickSize,
                        position,
                        _service);

                    _ = _service.TrySubscribeToPriceAsync(position.Symbol,
                        action: candle => closePositionTracker.OnNext(new CandleId(position.Symbol, _interval, candle.OpenTime)));

                    _ = _closePositionTrackers.TryAdd(position.Symbol, closePositionTracker);
                }
                else
                {
                    if (_closePositionTrackers.TryRemove(position.Symbol, out var takeProfit))
                    {
                        _ = _service.TryUnsubscribeFromPriceAsync(position.Symbol);
                    }
                }
            }
        }
    }
}
