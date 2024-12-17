using System.Runtime.InteropServices;
using MediatR;

namespace TradingAssistant
{
    public class TradingSignalWorker : BackgroundService
    {
        private readonly ILogger<TradingSignalWorker> _logger;
        private readonly TradingSignalQueueService _service;
        private readonly ISender _sender;
        private bool _hasReceivedSignalRecently = true;

        public TradingSignalWorker(ILogger<TradingSignalWorker> logger,
            TradingSignalQueueService service,
            ISender sender)
        {
            _logger = logger;
            _service = service;
            _sender = sender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_service.TryDequeue(out var signal))
                    {
                        _logger.LogInformation("Processing trading signal for {Symbol}", signal.Symbol);

                        if (_hasReceivedSignalRecently)
                        {
                            //await Task.Delay(1_000, stoppingToken);

                            _hasReceivedSignalRecently = false;
                        }

                        var hasTraded = await _sender.Send(new TradeRequest(signal.Symbol,
                                signal.TimeFrame,
                                signal.Time,
                                signal.Direction,
                                signal.Side,
                                signal.EntryPrice),
                            stoppingToken);

                        if (hasTraded)
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                Console.Beep(frequency: 250, duration: 250);
                            }

                            _logger.LogInformation("{Symbol} traded{NewLine}" +
                                "Waiting some seconds to avoid multiple positions",
                                signal.Symbol, Environment.NewLine);
                        }
                    }
                    else
                    {
                        _hasReceivedSignalRecently = true;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to process a trading signal");

                    await Task.Delay(1_000, stoppingToken);
                }
            }
        }
    }
}
