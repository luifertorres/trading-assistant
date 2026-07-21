using Binance.Net.Enums;

namespace TradingAssistant;

/// <summary>
/// Drives indicator computation and backfill size from <c>Binance:Indicators</c>.
/// Lengths are CSV strings (e.g. "5" or "5, 10, 20, 50, 100, 200") so appsettings layers replace cleanly.
/// To re-enable MeanReversion/TrendFollowing: set Lengths to "5, 10, 20, 50, 100, 200" and ComputeHigherTimeFrameSmas to true.
/// </summary>
internal sealed class IndicatorPipelineConfig
{
    private static readonly int[] DefaultLengths = [5, 10, 20, 50, 100, 200];

    public int[] Lengths { get; }
    public bool ComputeHigherTimeFrameSmas { get; }
    public int CandlestickSize { get; }

    public IndicatorPipelineConfig(IConfiguration configuration, KlineInterval timeFrame)
    {
        Lengths = ReadLengths(configuration, timeFrame);
        ComputeHigherTimeFrameSmas = configuration.GetValue("Binance:Indicators:ComputeHigherTimeFrameSmas", true);
        CandlestickSize = ResolveCandlestickSize(timeFrame, Lengths);
    }

    public static int ResolveCandlestickSize(KlineInterval timeFrame, int[] lengths)
    {
        var effectiveLengths = lengths.Length > 0 ? lengths : DefaultLengths;
        var structuralStopLossBars = Math.Max(60 * 60 * 24 / (int)timeFrame, 2);
        var maxLength = effectiveLengths.Max();
        var indicatorBars = 10 * maxLength + maxLength;

        return Math.Max(structuralStopLossBars, indicatorBars);
    }

    private static int[] ReadLengths(IConfiguration configuration, KlineInterval timeFrame)
    {
        var key = GetIndicatorLengthsKey(timeFrame);
        var path = $"Binance:Indicators:Lengths:{key}";
        var section = configuration.GetSection(path);

        // Prefer CSV/string so appsettings layers replace the whole value.
        // JSON arrays merge by index across files (e.g. Dev [5] + base [...,12000] → broken).
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            var parsed = ParseLengthCsv(section.Value);
            return parsed.Length > 0 ? parsed : DefaultLengths;
        }

        var lengths = section.Get<int[]>();
        return lengths is { Length: > 0 } ? lengths : DefaultLengths;
    }

    private static int[] ParseLengthCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var length) ? length : 0)
            .Where(length => length > 0)
            .ToArray();

    private static string GetIndicatorLengthsKey(KlineInterval timeFrame) => timeFrame switch
    {
        KlineInterval.OneMinute => "1m",
        KlineInterval.ThreeMinutes => "3m",
        KlineInterval.FiveMinutes => "5m",
        KlineInterval.FifteenMinutes => "15m",
        KlineInterval.OneHour => "1H",
        KlineInterval.FourHour => "4H",
        KlineInterval.OneDay => "1D",
        _ => timeFrame.ToString(),
    };
}
