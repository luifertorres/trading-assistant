namespace Portfolio;

public readonly record struct DrawdownEpisode(
    int PeakIndex,
    int TroughIndex,
    int? RecoveryIndex,
    double Depth);

public static class DrawdownEpisodes
{
    public static IReadOnlyList<DrawdownEpisode> Find(IReadOnlyList<UsdtPoint> series)
    {
        if (series.Count == 0)
            return [];

        var episodes = new List<DrawdownEpisode>();
        var peakIndex = 0;
        var peakUsdt = series[0].Usdt;
        int? episodePeakIndex = null;
        var troughIndex = 0;
        var troughUsdt = int.MaxValue;

        for (var i = 1; i < series.Count; i++)
        {
            var usdt = series[i].Usdt;
            if (usdt > peakUsdt)
            {
                if (episodePeakIndex is not null)
                {
                    var peak = series[episodePeakIndex.Value].Usdt;
                    episodes.Add(new DrawdownEpisode(
                        episodePeakIndex.Value,
                        troughIndex,
                        i,
                        (double)troughUsdt / peak - 1));
                    episodePeakIndex = null;
                }

                peakIndex = i;
                peakUsdt = usdt;
                continue;
            }

            if (usdt < peakUsdt)
            {
                if (episodePeakIndex is null)
                {
                    episodePeakIndex = peakIndex;
                    troughIndex = i;
                    troughUsdt = usdt;
                }
                else if (usdt < troughUsdt)
                {
                    troughIndex = i;
                    troughUsdt = usdt;
                }
            }
        }

        if (episodePeakIndex is not null)
        {
            var peak = series[episodePeakIndex.Value].Usdt;
            episodes.Add(new DrawdownEpisode(
                episodePeakIndex.Value,
                troughIndex,
                null,
                (double)troughUsdt / peak - 1));
        }

        return episodes;
    }
}
