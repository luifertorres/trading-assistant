namespace Portfolio;

public readonly record struct PairDependenceRow(
    ChartLegendId Left,
    ChartLegendId Right,
    double? DailyPearson,
    double? MonthlyPearson,
    double TailCoExceedance,
    double UnderwaterPearson,
    double? CoUnderwaterLift,
    double RightDuringLeftWorst,
    double LeftDuringRightWorst);

public readonly record struct DependenceReport(
    IReadOnlyList<PairDependenceRow> Pairs,
    DrawdownProfile Portfolio,
    DrawdownProfile MemberAverage)
{
    public static DependenceReport Build(IReadOnlyDictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> walks)
    {
        var walkIds = walks.Keys
            .Where(id => id != ChartLegendId.Sum)
            .OrderBy(id => id)
            .ToArray();

        var pairs = new List<PairDependenceRow>();
        for (var i = 0; i < walkIds.Length; i++)
        {
            for (var j = i + 1; j < walkIds.Length; j++)
            {
                var leftId = walkIds[i];
                var rightId = walkIds[j];
                var left = walks[leftId];
                var right = walks[rightId];
                pairs.Add(new PairDependenceRow(
                    leftId,
                    rightId,
                    SeriesCorrelation.LogReturnPearson(left, right),
                    SeriesCorrelation.LogReturnPearson(left, right, horizonDays: 30),
                    PairDependence.TailCoExceedance(left, right),
                    PairDependence.UnderwaterPearson(left, right),
                    PairDependence.CoUnderwaterLift(left, right),
                    PairDependence.ReturnDuringWorstEpisodes(left, right),
                    PairDependence.ReturnDuringWorstEpisodes(right, left)));
            }
        }

        var memberWalks = walkIds.Select(id => walks[id]).ToArray();
        var portfolio = PortfolioDrawdown.Profile(PortfolioDrawdown.EqualWeightIndex(memberWalks));
        var memberProfiles = memberWalks.Select(PortfolioDrawdown.Profile).ToArray();
        var memberAverage = new DrawdownProfile(
            memberProfiles.Average(p => p.MaxDrawdown),
            memberProfiles.Average(p => p.UlcerIndex),
            memberProfiles.Average(p => p.Cdar95),
            memberProfiles.Average(p => p.LongestUnderwaterDays));

        return new DependenceReport(pairs, portfolio, memberAverage);
    }

    public IReadOnlyList<string> FormatLines()
    {
        var lines = new List<string>(Pairs.Count + 1);
        foreach (var row in Pairs)
        {
            lines.Add(
                $"{ShortLabel(row.Left)}x{ShortLabel(row.Right)}  " +
                $"rho1d {FormatSigned(row.DailyPearson)}  " +
                $"rho30d {FormatSigned(row.MonthlyPearson)}  " +
                $"tail5% {row.TailCoExceedance:F2}  " +
                $"uwRho {row.UnderwaterPearson:F2}  " +
                $"lift-10% {FormatLift(row.CoUnderwaterLift)}  " +
                $"{ShortLabel(row.Right)}|{ShortLabel(row.Left)}worst3 {FormatPercent(row.RightDuringLeftWorst)}  " +
                $"{ShortLabel(row.Left)}|{ShortLabel(row.Right)}worst3 {FormatPercent(row.LeftDuringRightWorst)}");
        }

        lines.Add(
            $"portfolio maxDD {FormatPercent(Portfolio.MaxDrawdown)} ulcer {Portfolio.UlcerIndex:F3} cdar95 {FormatPercent(Portfolio.Cdar95)} longestUW {Portfolio.LongestUnderwaterDays:F0}d | " +
            $"member avg maxDD {FormatPercent(MemberAverage.MaxDrawdown)} ulcer {MemberAverage.UlcerIndex:F3} cdar95 {FormatPercent(MemberAverage.Cdar95)} longestUW {MemberAverage.LongestUnderwaterDays:F0}d");

        return lines;
    }

    private static string ShortLabel(ChartLegendId id) => id switch
    {
        ChartLegendId.Seed42 => "42",
        ChartLegendId.Seed7 => "7",
        ChartLegendId.Seed13 => "13",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static string FormatSigned(double? value) =>
        value is null ? "n/a" : $"{value.Value:+0.00;-0.00}";

    private static string FormatLift(double? value) =>
        value is null ? "n/a" : $"{value.Value:F2}";

    private static string FormatPercent(double fraction) => $"{fraction * 100:F1}%";
}
