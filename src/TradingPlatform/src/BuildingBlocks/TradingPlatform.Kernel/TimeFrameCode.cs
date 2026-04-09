namespace TradingPlatform.Kernel;

/// <summary>
/// Chart-style timeframe codes: lowercase <b>s</b>/<b>m</b> below 1 hour; uppercase <b>H</b>/<b>D</b>/<b>W</b>/<b>M</b> at hour and above.
/// <b>1m</b> = 1 minute and <b>1M</b> = 1 month are distinguished by case (TradingView / Binance convention).
/// </summary>
public readonly record struct TimeFrameCode(string Value)
{
    public static readonly TimeFrameCode Sec1 = new("1s");

    public static readonly TimeFrameCode Min1 = new("1m");
    public static readonly TimeFrameCode Min3 = new("3m");
    public static readonly TimeFrameCode Min5 = new("5m");
    public static readonly TimeFrameCode Min15 = new("15m");
    public static readonly TimeFrameCode Min30 = new("30m");

    public static readonly TimeFrameCode Hour1 = new("1H");
    public static readonly TimeFrameCode Hour2 = new("2H");
    public static readonly TimeFrameCode Hour4 = new("4H");
    public static readonly TimeFrameCode Hour6 = new("6H");
    public static readonly TimeFrameCode Hour8 = new("8H");
    public static readonly TimeFrameCode Hour12 = new("12H");

    public static readonly TimeFrameCode Day1 = new("1D");
    public static readonly TimeFrameCode Week1 = new("1W");
    public static readonly TimeFrameCode Month1 = new("1M");

    public static TimeFrameCode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Time frame is required.", nameof(raw));

        var s = raw.Trim();

        // Long-form / legacy aliases (always unambiguous)
        switch (s.ToLowerInvariant())
        {
            case "1min":
            case "m1":
                return Min1;
            case "3min":
            case "m3":
                return Min3;
            case "5min":
            case "m5":
                return Min5;
            case "15min":
            case "m15":
                return Min15;
            case "30min":
            case "m30":
                return Min30;
            case "1second":
            case "1sec":
            case "s1":
                return Sec1;
            case "1hour":
            case "h1":
                return Hour1;
            case "2hour":
            case "2hours":
            case "h2":
                return Hour2;
            case "4hour":
            case "4hours":
            case "h4":
                return Hour4;
            case "6hour":
            case "6hours":
            case "h6":
                return Hour6;
            case "8hour":
            case "8hours":
            case "h8":
                return Hour8;
            case "12hour":
            case "12hours":
            case "h12":
                return Hour12;
            case "1day":
            case "d1":
                return Day1;
            case "1week":
            case "w1":
                return Week1;
            case "1month":
            case "1mo":
            case "mn1":
                return Month1;
        }

        // Chart codes: preserve m vs M; normalize H/D/W/s casing to match the UI
        return s switch
        {
            "1s" or "1S" => Sec1,
            "1m" => Min1,
            "1M" => Month1,
            "3m" => Min3,
            "5m" => Min5,
            "15m" => Min15,
            "30m" => Min30,
            "1H" or "1h" => Hour1,
            "2H" or "2h" => Hour2,
            "4H" or "4h" => Hour4,
            "6H" or "6h" => Hour6,
            "8H" or "8h" => Hour8,
            "12H" or "12h" => Hour12,
            "1D" or "1d" => Day1,
            "1W" or "1w" => Week1,
            _ => new TimeFrameCode(NormalizeCustom(s))
        };
    }

    /// <summary>Best-effort normalization for unknown codes used in table names (H/D/W uppercase, minutes lowercase m, month uppercase M).</summary>
    private static string NormalizeCustom(string s)
    {
        if (s.Length < 2)
            return s;

        var last = s[^1];
        var prefix = s[..^1];
        if (!prefix.All(char.IsAsciiDigit))
            return s;

        return last switch
        {
            'h' or 'H' => prefix + "H",
            'd' or 'D' => prefix + "D",
            'w' or 'W' => prefix + "W",
            's' or 'S' => prefix + "s",
            'm' => prefix + "m",
            'M' => prefix + "M",
            _ => s
        };
    }
}
