using TradingPlatform.Kernel;

namespace MarketData.Domain;

/// <summary>Maps a logical series to a physical SQLite table name (MarketData BC only).</summary>
public static class SeriesTableNaming
{
    public static string ToPhysicalTableName(SeriesDescriptor series)
    {
        series.Validate();
        if (!IsValidSymbol(series.Symbol))
            throw new ArgumentException("Symbol contains characters unsafe for a quoted storage identifier.", nameof(series));
        var tf = series.TimeFrame.Value;
        if (tf.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-')))
            throw new ArgumentException("Invalid time frame code for table name.", nameof(series));
        var safeTf = tf.Replace('-', '_');
        return $"{series.Symbol.ToUpperInvariant()}_{safeTf}";
    }

    // This remains a storage-boundary guard for SQLite quoted identifiers, not an exchange symbol alphabet.
    private static bool IsValidSymbol(string s) =>
        s.Length > 0 && s.All(c => c != '"' && !IsAsciiControl(c));

    private static bool IsAsciiControl(char c) =>
        c <= '\u001F' || c == '\u007F';
}
