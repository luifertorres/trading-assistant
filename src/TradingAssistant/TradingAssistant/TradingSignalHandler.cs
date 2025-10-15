using MediatR;
using TradingAssistant.Application;

namespace TradingAssistant
{
    public class TradingSignalHandler : INotificationHandler<TradingSignalNotification>
    {
        private readonly ILogger<TradingSignalHandler> _logger;
        private readonly TradingSignalQueueService _service;

        public TradingSignalHandler(ILogger<TradingSignalHandler> logger, TradingSignalQueueService service)
        {
            _logger = logger;
            _service = service;
        }

        public Task Handle(TradingSignalNotification signal, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Symbol} signal enqueued", signal.Symbol);
            _service.Enqueue(signal);

            return Task.CompletedTask;
        }
    }
}
