using System.Collections.Concurrent;

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

        public void ClearQueue() => _signals.Clear();
    }
}
