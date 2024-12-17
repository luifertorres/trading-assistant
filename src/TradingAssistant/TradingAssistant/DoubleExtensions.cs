namespace TradingAssistant
{
    internal static class DoubleExtensions
    {
        public static readonly Index Penultimate = ^2;
        public static readonly Index Last = ^1;
        private const int MinimumLookbackPeriods = 2;

        internal static bool AreUptrending(this IEnumerable<double[]> series)
        {
            return series.All(data => data.Length >= 2) && series.All(data => data[Last] > data[Penultimate]);
        }

        internal static bool AreDowntrending(this IEnumerable<double[]> series)
        {
            return series.All(data => data.Length >= 2) && series.All(data => data[Last] < data[Penultimate]);
        }

        internal static IEnumerable<double> PickLatestValues(this IEnumerable<double[]> series)
        {
            return series.Select(value => value[Last]);
        }

        internal static bool WereOrderedFromSlowToFast(this IEnumerable<double[]> series, int lookbackPeriods)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(lookbackPeriods, MinimumLookbackPeriods);

            var wereOrderedFromSlowestToFastest = false;

            for (var i = MinimumLookbackPeriods; i <= lookbackPeriods; i++)
            {
                if (series.Any(rsi => rsi.Length < i))
                {
                    return false;
                }

                var previousSeries = series.Select(rsi => rsi[^i]);

                wereOrderedFromSlowestToFastest = previousSeries.Order().SequenceEqual(previousSeries);

                if (wereOrderedFromSlowestToFastest)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool WereOrderedFromFastToSlow(this IEnumerable<double[]> series, int lookbackPeriods)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(lookbackPeriods, MinimumLookbackPeriods);

            var wereOrderedFromFastestToSlowest = false;

            for (var i = MinimumLookbackPeriods; i <= lookbackPeriods; i++)
            {
                if (series.Any(rsi => rsi.Length < i))
                {
                    return false;
                }

                var previousSeries = series.Select(rsi => rsi[^i]);

                wereOrderedFromFastestToSlowest = previousSeries.OrderDescending().SequenceEqual(previousSeries);

                if (wereOrderedFromFastestToSlowest)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool AreOrderedFromSlowToFast(this IEnumerable<double> values)
        {
            return values.Order().SequenceEqual(values);
        }

        internal static bool AreOrderedFromFastToSlow(this IEnumerable<double> values)
        {
            return values.OrderDescending().SequenceEqual(values);
        }
    }
}
