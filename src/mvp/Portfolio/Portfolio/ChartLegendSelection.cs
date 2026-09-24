namespace Portfolio;

public enum ChartLegendId
{
    Seed42,
    Seed7,
    Sum
}

public sealed class ChartLegendSelection
{
    private bool _seed42Enabled = true;
    private bool _seed7Enabled = true;
    private bool _sumEnabled = true;

    public bool IsEnabled(ChartLegendId id) => id switch
    {
        ChartLegendId.Seed42 => _seed42Enabled,
        ChartLegendId.Seed7 => _seed7Enabled,
        ChartLegendId.Sum => _sumEnabled,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    public IReadOnlyList<ChartLegendId> Plotted
    {
        get
        {
            if (_sumEnabled)
                return [ChartLegendId.Sum];

            var plotted = new List<ChartLegendId>(2);
            if (_seed42Enabled)
                plotted.Add(ChartLegendId.Seed42);
            if (_seed7Enabled)
                plotted.Add(ChartLegendId.Seed7);

            return plotted;
        }
    }

    public bool TryToggle(ChartLegendId id)
    {
        return id switch
        {
            ChartLegendId.Sum => TryToggleSum(),
            ChartLegendId.Seed42 => TryToggleWalk(ref _seed42Enabled),
            ChartLegendId.Seed7 => TryToggleWalk(ref _seed7Enabled),
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
    }

    private bool TryToggleSum()
    {
        if (_sumEnabled)
        {
            if (!_seed42Enabled && !_seed7Enabled)
                return false;

            _sumEnabled = false;
            return true;
        }

        _sumEnabled = true;
        return true;
    }

    private bool TryToggleWalk(ref bool enabled)
    {
        if (_sumEnabled)
        {
            enabled = !enabled;
            return true;
        }

        if (enabled && Plotted.Count == 1)
            return false;

        enabled = !enabled;
        return true;
    }
}
