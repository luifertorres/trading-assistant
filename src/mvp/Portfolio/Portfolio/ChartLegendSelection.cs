namespace Portfolio;

public enum ChartLegendId
{
    Seed42,
    Seed7,
    Seed13,
    Sum
}

public sealed class ChartLegendSelection
{
    private readonly IReadOnlyList<ChartLegendId> _walkIds;
    private readonly Dictionary<ChartLegendId, bool> _walkEnabled;
    private bool _sumEnabled = true;

    public ChartLegendSelection()
        : this([ChartLegendId.Seed42, ChartLegendId.Seed7])
    {
    }

    public ChartLegendSelection(IReadOnlyList<ChartLegendId> walkIds)
    {
        if (walkIds.Count == 0)
            throw new ArgumentException("At least one walk id is required.", nameof(walkIds));

        _walkIds = walkIds.ToArray();
        _walkEnabled = walkIds.ToDictionary(id => id, _ => true);
    }

    public bool IsEnabled(ChartLegendId id) => id switch
    {
        ChartLegendId.Sum => _sumEnabled,
        _ when _walkEnabled.TryGetValue(id, out var enabled) => enabled,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    public IReadOnlyList<ChartLegendId> Plotted
    {
        get
        {
            if (_sumEnabled)
                return [ChartLegendId.Sum];

            var plotted = new List<ChartLegendId>(_walkIds.Count);
            foreach (var id in _walkIds)
            {
                if (_walkEnabled[id])
                    plotted.Add(id);
            }

            return plotted;
        }
    }

    public bool ShowCorrelation
    {
        get
        {
            if (_sumEnabled)
                return false;

            var individualCount = 0;
            foreach (var id in _walkIds)
            {
                if (_walkEnabled[id])
                    individualCount++;
            }

            return individualCount == 2;
        }
    }

    public bool TryToggle(ChartLegendId id)
    {
        return id switch
        {
            ChartLegendId.Sum => TryToggleSum(),
            _ when _walkEnabled.ContainsKey(id) => TryToggleWalk(id),
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
    }

    private bool TryToggleSum()
    {
        if (_sumEnabled)
        {
            if (!_walkIds.Any(id => _walkEnabled[id]))
                return false;

            _sumEnabled = false;
            return true;
        }

        _sumEnabled = true;
        return true;
    }

    private bool TryToggleWalk(ChartLegendId id)
    {
        if (_sumEnabled)
        {
            _walkEnabled[id] = !_walkEnabled[id];
            return true;
        }

        if (_walkEnabled[id] && EnabledWalkCount() == 1)
            return false;

        _walkEnabled[id] = !_walkEnabled[id];
        return true;
    }

    private int EnabledWalkCount()
    {
        var count = 0;
        foreach (var walkId in _walkIds)
        {
            if (_walkEnabled[walkId])
                count++;
        }

        return count;
    }
}
