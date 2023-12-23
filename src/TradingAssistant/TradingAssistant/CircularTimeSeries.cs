using System.Collections.Immutable;

namespace TradingAssistant
{
    public class CircularTimeSeries<T>(int maxSize)
    {
        private readonly int _maxSize = maxSize;
        private readonly SortedDictionary<DateTime, T> _series = [];

        public void Add(DateTime time, T value)
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

        public bool TryGetValue(DateTime time, out T? value)
        {
            if (!_series.TryGetValue(time, out value))
            {
                return false;
            }

            return true;
        }

        public List<T> Last(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _maxSize, nameof(count));

            return new List<T>(_series.TakeLast(count).Select(p => p.Value));
        }
    }
}
