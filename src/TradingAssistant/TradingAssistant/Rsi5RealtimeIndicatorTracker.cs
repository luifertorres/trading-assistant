using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using MediatR;
using Skender.Stock.Indicators;
using TradingAssistant.Application;
using ApplicationIndicatorType = TradingAssistant.Application.IndicatorType;

namespace TradingAssistant;

internal sealed class Rsi5RealtimeIndicatorTracker
{
    private const int Period = 5;
    private const int WarmupPeriods = Period * 10;
    private readonly string _symbol;
    private readonly KlineInterval _interval;
    private readonly IPublisher _publisher;
    private readonly CancellationToken _stoppingToken;
    private readonly double _threshold;
    private readonly List<Quote> _quotes = new();
    private readonly object _syncRoot = new();
    private DateTime? _lastSignalOpenTime;

    public Rsi5RealtimeIndicatorTracker(string symbol,
        KlineInterval interval,
        IPublisher publisher,
        CancellationToken stoppingToken,
        double threshold = 90.0)
    {
        _symbol = symbol;
        _interval = interval;
        _publisher = publisher;
        _stoppingToken = stoppingToken;
        _threshold = threshold;
    }

    public void OnNext(IBinanceKline kline)
    {
        _ = ProcessAsync(kline);
    }

    private async Task ProcessAsync(IBinanceKline kline)
    {
        if (_stoppingToken.IsCancellationRequested)
        {
            return;
        }

        double? latestRsi;
        DateTime openTime;
        decimal lastPrice;

        lock (_syncRoot)
        {
            UpsertQuote(kline);

            if (_quotes.Count < WarmupPeriods + Period)
            {
                return;
            }

            var rsi = _quotes.Validate()
                .TakeLast(WarmupPeriods + Period)
                .GetRsi(Period)
                .LastOrDefault()?.Rsi;

            if (!rsi.HasValue || rsi.Value < _threshold)
            {
                return;
            }

            openTime = kline.OpenTime;
            lastPrice = kline.ClosePrice;

            if (_lastSignalOpenTime.HasValue && _lastSignalOpenTime.Value == openTime)
            {
                return;
            }

            _lastSignalOpenTime = openTime;
            latestRsi = rsi.Value;
        }

        var conditionEvent = new IndicatorConditionMetNotification(_symbol,
            _interval,
            openTime,
            lastPrice,
            ApplicationIndicatorType.Rsi,
            Period,
            latestRsi.Value,
            _threshold,
            IndicatorComparison.GreaterThanOrEqual,
            IndicatorEventSource.RealTime);

        await _publisher.Publish(conditionEvent, _stoppingToken);
    }

    private void UpsertQuote(IBinanceKline kline)
    {
        var quote = new Quote
        {
            Date = kline.OpenTime,
            Open = kline.OpenPrice,
            High = kline.HighPrice,
            Low = kline.LowPrice,
            Close = kline.ClosePrice,
            Volume = kline.Volume
        };

        if (_quotes.Count > 0 && _quotes[^1].Date == quote.Date)
        {
            _quotes[^1] = quote;
        }
        else
        {
            _quotes.Add(quote);

            var maxBuffer = WarmupPeriods + Period + 5;

            if (_quotes.Count > maxBuffer)
            {
                _quotes.RemoveRange(0, _quotes.Count - maxBuffer);
            }
        }
    }
}


