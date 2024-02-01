using System.Collections.Concurrent;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;

namespace TradingAssistant
{
    public class Rsi200ClosePositionWorker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly BinanceService _service;
        private readonly ConcurrentDictionary<string, Rsi200ClosePositionTracker> _closePositionTrackers = [];

        public Rsi200ClosePositionWorker(IConfiguration configuration, BinanceService binanceService)
        {
            _configuration = configuration;
            _service = binanceService;
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
                    var candleClosedEvent = _service.GetCandleClosedEvent();

                    if (_closePositionTrackers.TryRemove(position.Symbol, out var closePositionTracker))
                    {
                        closePositionTracker.Unsubscribe();
                    }

                    closePositionTracker = new Rsi200ClosePositionTracker(position, _service);

                    closePositionTracker.SubscribeTo(candleClosedEvent);

                    _closePositionTrackers.TryAdd(position.Symbol, closePositionTracker);
                }
                else
                {
                    if (_closePositionTrackers.TryRemove(position.Symbol, out var takeProfit))
                    {
                        takeProfit.Unsubscribe();
                    }
                }
            }
        }
    }
}
