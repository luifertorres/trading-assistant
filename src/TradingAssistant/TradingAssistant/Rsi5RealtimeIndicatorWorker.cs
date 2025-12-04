using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingAssistant;

public class Rsi5RealtimeIndicatorWorker : BackgroundService
{
    private readonly ILogger<Rsi5RealtimeIndicatorWorker> _logger;
    private readonly BinanceService _binance;
    private readonly IPublisher _publisher;
    private readonly KlineInterval _interval;
    private readonly ConcurrentDictionary<string, Rsi5RealtimeIndicatorTracker> _trackers = new(StringComparer.OrdinalIgnoreCase);

    public Rsi5RealtimeIndicatorWorker(ILogger<Rsi5RealtimeIndicatorWorker> logger,
        IConfiguration configuration,
        BinanceService binance,
        IPublisher publisher)
    {
        _logger = logger;
        _binance = binance;
        _publisher = publisher;
        _interval = configuration.GetValue<KlineInterval>("Binance:Service:TimeFrameSeconds");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _binance.SubscribeToAccountUpdates(@event => HandleAccountUpdate(@event, stoppingToken));

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event, CancellationToken stoppingToken)
    {
        foreach (var position in @event.Data.UpdateData.Positions)
        {
            var symbol = position.Symbol;

            if (SymbolExclusions.Contains(symbol))
            {
                if (_trackers.TryRemove(symbol, out _))
                {
                    _ = _binance.TryUnsubscribeFromPriceAsync(symbol);
                }

                continue;
            }

            var hasOpenPosition = position.EntryPrice != 0 && position.Quantity != 0;

            if (hasOpenPosition)
            {
                if (_trackers.ContainsKey(symbol))
                {
                    continue;
                }

                var tracker = new Rsi5RealtimeIndicatorTracker(symbol,
                    _interval,
                    _publisher,
                    stoppingToken,
                    [50.0, 90.0]);

                if (_trackers.TryAdd(symbol, tracker))
                {
                    _ = _binance.TrySubscribeToPriceAsync(symbol, tracker.OnNext);
                    _logger.LogInformation("Subscribed RSI(5) real-time tracker for {Symbol}", symbol);
                }
            }
            else
            {
                if (_trackers.TryRemove(symbol, out _))
                {
                    _ = _binance.TryUnsubscribeFromPriceAsync(symbol);
                    _logger.LogInformation("Stopped RSI(5) real-time tracker for {Symbol}", symbol);
                }
            }
        }
    }
}


