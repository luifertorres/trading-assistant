using Execution.Application;
using MarketData.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Research.Application;
using TradingPlatform.Kernel;

namespace TradingPlatform.Host;

/// <summary>Evaluates Rsi5Extreme on closed 4H candles for cohort symbols; replays last closed bar at startup.</summary>
public sealed class LiveStrategyWorker(
    ILiveCandleFeed feed,
    ITradingStrategyFactory strategies,
    ILiveOrderIntentSink liveSink,
    ILogger<LiveStrategyWorker> log) : BackgroundService
{
    private static readonly string[] Symbols = ["DOGEUSDT", "XRPUSDT", "SOLUSDT", "1000PEPEUSDT"];
    private static readonly TimeFrameCode TimeFrame = TimeFrameCode.Hour4;
    private readonly Dictionary<string, (TradingVectorSpec Spec, ITradingStrategy Strategy, int BarCount)> _state = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("LiveStrategyWorker starting for {Count} symbols on {Tf}.", Symbols.Length, TimeFrame.Value);

        async Task OnCandle(ClosedCandleEvent evt, CancellationToken ct)
        {
            var symbol = evt.ExchangeSymbol;
            if (!_state.TryGetValue(symbol, out var entry))
            {
                var spec = new TradingVectorSpec(
                    TradingVectorId.New(),
                    Asset.FromUsdmExchangeSymbol(symbol),
                    evt.InstrumentId,
                    TimeFrame,
                    Direction.Long,
                    "Rsi5Extreme",
                    new Dictionary<string, string> { ["takeProfitPct"] = "0.08", ["rsiExit"] = "70" });
                entry = (spec, strategies.Create(spec), 0);
                _state[symbol] = entry;
            }

            var barIndex = entry.BarCount++;
            var adapter = new LiveBarSink(liveSink);
            entry.Strategy.OnBar(new BarProcessingContext
            {
                Bar = evt.Bar,
                BarIndex = barIndex,
                Vector = entry.Spec,
                Sink = adapter
            });
            await adapter.FlushAsync(ct).ConfigureAwait(false);
            log.LogInformation("Evaluated {Symbol} closed {CloseTime} close={Close:F4}", symbol, evt.Bar.CloseTime, evt.Bar.Close);
        }

        await feed.ReplayLastClosedAsync(Symbols, TimeFrame, OnCandle, stoppingToken).ConfigureAwait(false);
        await feed.SubscribeAsync(Symbols, TimeFrame, OnCandle, stoppingToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private sealed class LiveBarSink(ILiveOrderIntentSink live) : ISimulationOrderIntentSink
    {
        private readonly Queue<OrderIntent> _pending = new();

        public void OnIntent(in OrderIntent intent, in OhlcBar signalBar) => _pending.Enqueue(intent);

        public void OnBarClosed(in OhlcBar bar)
        {
        }

        public async Task FlushAsync(CancellationToken ct)
        {
            while (_pending.Count > 0)
                await live.OnIntentAsync(_pending.Dequeue(), ct).ConfigureAwait(false);
        }
    }
}
