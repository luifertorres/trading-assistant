using System.Collections.Concurrent;
using TradingAssistant.Application;

namespace TradingAssistant
{
    public class TradingSignalQueueService
    {
        private readonly ConcurrentQueue<TradingSignalNotification> _signals = [];

        public void Enqueue(TradingSignalNotification signal)
        {
            ArgumentNullException.ThrowIfNull(signal);

            _signals.Enqueue(signal);
        }

        public bool TryDequeue(out TradingSignalNotification signal) => _signals.TryDequeue(out signal!);

        public void Clear() => _signals.Clear();
    }
}
