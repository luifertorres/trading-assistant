using System.Collections.Immutable;

namespace TradingAssistant
{
    public class CircularTimeSeries<TKey, TValue>(TKey symbol, int maxSize)
    {
        private readonly TKey _symbol = symbol;
        private readonly int _maxSize = maxSize;
        private readonly SortedDictionary<DateTime, TValue> _series = [];

        public TKey Symbol => _symbol;

        public void Add(DateTime time, TValue value)
        {
            _series[time] = value;
            EnsureMaxSize();
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

            return _series.TakeLast(count).Select(p => p.Value).ToList();
        }

        public List<TValue> Snapshot() => _series.Select(p => p.Value).ToList();
    }
}
