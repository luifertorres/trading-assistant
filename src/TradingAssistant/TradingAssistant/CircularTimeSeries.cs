using System.Collections.Immutable;

namespace TradingAssistant
{
    public class CircularTimeSeries<TKey, TValue>
    {
        private static readonly object s_synchronizer = new();
        private readonly TKey _symbol;
        private readonly int _maxSize;
        private readonly SortedDictionary<DateTime, TValue> _series = [];

        public TKey Symbol => _symbol;

        public CircularTimeSeries(TKey symbol, int maxSize)
        {
            _symbol = symbol;
            _maxSize = maxSize;
        }

        public void Add(DateTime time, TValue value)
        {
            lock (s_synchronizer)
            {
                _series[time] = value;
                EnsureMaxSize();
            }
        }

        private void EnsureMaxSize()
        {
            while (_series.Count > _maxSize)
            {
                var oldest = _series.Keys.FirstOrDefault();

                _series.Remove(oldest, out _);
            }
        }

        public bool TryGetValue(DateTime time, out TValue? value)
        {
            if (!_series.TryGetValue(time, out value))
            {
                return false;
            }

            return true;
        }

        public List<TValue> Last(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _maxSize, nameof(count));

            lock (s_synchronizer)
            {
                return _series.TakeLast(count).Select(p => p.Value).ToList();
            }
        }

        public List<TValue> Snapshot()
        {
            lock (s_synchronizer)
            {
                return _series.Select(p => p.Value).ToList();
            }
        }
    }
}
