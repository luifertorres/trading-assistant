using TradingPlatform.Kernel;

namespace MarketData.Domain;

/// <summary>Maps a logical series to a physical SQLite table name (MarketData BC only).</summary>
public static class SeriesTableNaming
{
    public static string ToPhysicalTableName(SeriesDescriptor series)
    {
        series.Validate();
        if (!IsValidSymbol(series.Symbol))
            throw new ArgumentException("Symbol must be alphanumeric.", nameof(series));
        var tf = series.TimeFrame.Value;
        if (tf.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-')))
            throw new ArgumentException("Invalid time frame code for table name.", nameof(series));
        var safeTf = tf.Replace('-', '_');
        return $"{series.Symbol.ToUpperInvariant()}_{safeTf}";
    }

    private static bool IsValidSymbol(string s) =>
        s.Length > 0 && s.All(c => char.IsAsciiLetterOrDigit(c));
}
