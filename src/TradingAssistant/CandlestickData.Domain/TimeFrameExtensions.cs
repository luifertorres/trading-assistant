namespace CandlestickData.Domain;

public static class TimeFrameExtensions
{
    public static TimeSpan ToTimeSpan(this TimeFrame timeFrame) => timeFrame switch
    {
        TimeFrame.OneMinute => TimeSpan.FromMinutes(1),
        TimeFrame.ThreeMinutes => TimeSpan.FromMinutes(3),
        TimeFrame.FiveMinutes => TimeSpan.FromMinutes(5),
        TimeFrame.FifteenMinutes => TimeSpan.FromMinutes(15),
        TimeFrame.ThirtyMinutes => TimeSpan.FromMinutes(30),
        TimeFrame.OneHour => TimeSpan.FromHours(1),
        TimeFrame.TwoHours => TimeSpan.FromHours(2),
        TimeFrame.FourHours => TimeSpan.FromHours(4),
        TimeFrame.SixHours => TimeSpan.FromHours(6),
        TimeFrame.EightHours => TimeSpan.FromHours(8),
        TimeFrame.TwelveHours => TimeSpan.FromHours(12),
        TimeFrame.OneDay => TimeSpan.FromDays(1),
        TimeFrame.ThreeDays => TimeSpan.FromDays(3),
        TimeFrame.OneWeek => TimeSpan.FromDays(7),
        TimeFrame.OneMonth => TimeSpan.FromDays(30),
        _ => throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, null)
    };

    public static string ToShortString(this TimeFrame timeFrame) => timeFrame switch
    {
        TimeFrame.OneMinute => "1m",
        TimeFrame.ThreeMinutes => "3m",
        TimeFrame.FiveMinutes => "5m",
        TimeFrame.FifteenMinutes => "15m",
        TimeFrame.ThirtyMinutes => "30m",
        TimeFrame.OneHour => "1h",
        TimeFrame.TwoHours => "2h",
        TimeFrame.FourHours => "4h",
        TimeFrame.SixHours => "6h",
        TimeFrame.EightHours => "8h",
        TimeFrame.TwelveHours => "12h",
        TimeFrame.OneDay => "1d",
        TimeFrame.ThreeDays => "3d",
        TimeFrame.OneWeek => "1w",
        TimeFrame.OneMonth => "1M",
        _ => throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, null)
    };

    public static bool TryParseFromShortString(string value, out TimeFrame timeFrame)
    {
        timeFrame = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var (matched, tf) = trimmed switch
        {
            "1m" => (true, TimeFrame.OneMinute),
            "3m" => (true, TimeFrame.ThreeMinutes),
            "5m" => (true, TimeFrame.FiveMinutes),
            "15m" => (true, TimeFrame.FifteenMinutes),
            "30m" => (true, TimeFrame.ThirtyMinutes),
            "1h" => (true, TimeFrame.OneHour),
            "2h" => (true, TimeFrame.TwoHours),
            "4h" => (true, TimeFrame.FourHours),
            "6h" => (true, TimeFrame.SixHours),
            "8h" => (true, TimeFrame.EightHours),
            "12h" => (true, TimeFrame.TwelveHours),
            "1d" => (true, TimeFrame.OneDay),
            "3d" => (true, TimeFrame.ThreeDays),
            "1w" => (true, TimeFrame.OneWeek),
            "1M" => (true, TimeFrame.OneMonth),
            _ => (false, default)
        };

        timeFrame = tf;
        return matched;
    }
}
