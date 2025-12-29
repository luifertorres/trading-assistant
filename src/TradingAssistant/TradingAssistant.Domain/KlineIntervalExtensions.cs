using Binance.Net.Enums;

namespace TradingAssistant;

public static class KlineIntervalExtensions
{
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

