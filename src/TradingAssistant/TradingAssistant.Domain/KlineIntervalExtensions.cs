using Binance.Net.Enums;

namespace TradingAssistant;

public static class KlineIntervalExtensions
{
    public static bool TryParseFromShortString(string value, out KlineInterval interval)
    {
        interval = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        interval = trimmed switch
        {
            "1m" => KlineInterval.OneMinute,
            "3m" => KlineInterval.ThreeMinutes,
            "5m" => KlineInterval.FiveMinutes,
            "15m" => KlineInterval.FifteenMinutes,
            "30m" => KlineInterval.ThirtyMinutes,
            "1h" => KlineInterval.OneHour,
            "2h" => KlineInterval.TwoHour,
            "4h" => KlineInterval.FourHour,
            "6h" => KlineInterval.SixHour,
            "8h" => KlineInterval.EightHour,
            "12h" => KlineInterval.TwelveHour,
            "1d" => KlineInterval.OneDay,
            "3d" => KlineInterval.ThreeDay,
            "1w" => KlineInterval.OneWeek,
            "1M" => KlineInterval.OneMonth,
            _ => default
        };

        return interval != default;
    }

    public static int ToSeconds(this KlineInterval interval)
    {
        return interval switch
        {
            KlineInterval.OneMinute => 60,
            KlineInterval.ThreeMinutes => 180,
            KlineInterval.FiveMinutes => 300,
            KlineInterval.FifteenMinutes => 900,
            KlineInterval.ThirtyMinutes => 1800,
            KlineInterval.OneHour => 3600,
            KlineInterval.TwoHour => 7200,
            KlineInterval.FourHour => 14400,
            KlineInterval.SixHour => 21600,
            KlineInterval.EightHour => 28800,
            KlineInterval.TwelveHour => 43200,
            KlineInterval.OneDay => 86400,
            KlineInterval.ThreeDay => 259200,
            KlineInterval.OneWeek => 604800,
            KlineInterval.OneMonth => 2592000,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
}

