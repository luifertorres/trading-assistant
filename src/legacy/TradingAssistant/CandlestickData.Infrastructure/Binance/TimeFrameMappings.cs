using Binance.Net.Enums;
using CandlestickData.Domain;

namespace CandlestickData.Infrastructure.Binance;

internal static class TimeFrameMappings
{
    public static KlineInterval ToKlineInterval(this TimeFrame timeFrame) => timeFrame switch
    {
        TimeFrame.OneMinute => KlineInterval.OneMinute,
        TimeFrame.ThreeMinutes => KlineInterval.ThreeMinutes,
        TimeFrame.FiveMinutes => KlineInterval.FiveMinutes,
        TimeFrame.FifteenMinutes => KlineInterval.FifteenMinutes,
        TimeFrame.ThirtyMinutes => KlineInterval.ThirtyMinutes,
        TimeFrame.OneHour => KlineInterval.OneHour,
        TimeFrame.TwoHours => KlineInterval.TwoHour,
        TimeFrame.FourHours => KlineInterval.FourHour,
        TimeFrame.SixHours => KlineInterval.SixHour,
        TimeFrame.EightHours => KlineInterval.EightHour,
        TimeFrame.TwelveHours => KlineInterval.TwelveHour,
        TimeFrame.OneDay => KlineInterval.OneDay,
        TimeFrame.ThreeDays => KlineInterval.ThreeDay,
        TimeFrame.OneWeek => KlineInterval.OneWeek,
        TimeFrame.OneMonth => KlineInterval.OneMonth,
        _ => throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, null)
    };

    public static TimeFrame ToTimeFrame(this KlineInterval interval) => interval switch
    {
        KlineInterval.OneMinute => TimeFrame.OneMinute,
        KlineInterval.ThreeMinutes => TimeFrame.ThreeMinutes,
        KlineInterval.FiveMinutes => TimeFrame.FiveMinutes,
        KlineInterval.FifteenMinutes => TimeFrame.FifteenMinutes,
        KlineInterval.ThirtyMinutes => TimeFrame.ThirtyMinutes,
        KlineInterval.OneHour => TimeFrame.OneHour,
        KlineInterval.TwoHour => TimeFrame.TwoHours,
        KlineInterval.FourHour => TimeFrame.FourHours,
        KlineInterval.SixHour => TimeFrame.SixHours,
        KlineInterval.EightHour => TimeFrame.EightHours,
        KlineInterval.TwelveHour => TimeFrame.TwelveHours,
        KlineInterval.OneDay => TimeFrame.OneDay,
        KlineInterval.ThreeDay => TimeFrame.ThreeDays,
        KlineInterval.OneWeek => TimeFrame.OneWeek,
        KlineInterval.OneMonth => TimeFrame.OneMonth,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };
}
