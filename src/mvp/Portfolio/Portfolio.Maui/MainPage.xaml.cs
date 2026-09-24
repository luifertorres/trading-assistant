using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;

namespace Portfolio.Maui;

public partial class MainPage : ContentPage
{
    private const double HiddenLegendOpacity = 0.4;

    private readonly IReadOnlyList<UsdtPoint> _seed42Series;
    private readonly IReadOnlyList<UsdtPoint> _seed7Series;
    private readonly IReadOnlyList<UsdtPoint> _sumSeries;
    private readonly ChartLegendSelection _legendSelection = new();
    private readonly Dictionary<ChartLegendId, Scatter> _scatters = new();
    private Crosshair? _crosshair;

    public MainPage()
    {
        InitializeComponent();
        _seed42Series = UtcMinusFiveUsdtSeries.Generate(UtcMinusFiveUsdtSeries.DefaultSeed);
        _seed7Series = UtcMinusFiveUsdtSeries.Generate(UtcMinusFiveUsdtSeries.SecondSeed);
        _sumSeries = UtcMinusFiveUsdtSeries.Sum(_seed42Series, _seed7Series);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_crosshair is not null)
            return;

        BuildChart();
    }

    private void BuildChart()
    {
        _scatters[ChartLegendId.Seed42] = AddSeries(_seed42Series, ScottPlot.Colors.Blue);
        _scatters[ChartLegendId.Seed7] = AddSeries(_seed7Series, ScottPlot.Colors.Orange);
        _scatters[ChartLegendId.Sum] = AddSeries(_sumSeries, ScottPlot.Colors.Green);

        Chart.Plot.Axes.Bottom.Label.Text = "UTC-5";
        Chart.Plot.Axes.Left.Label.Text = "USDT";
        ApplyCustomTicks();
        ApplyLegendState();

        _crosshair = Chart.Plot.Add.Crosshair(0, 0);
        _crosshair.IsVisible = false;

        Chart.Refresh();

        var gesture = new PointerGestureRecognizer();
        gesture.PointerMoved += OnPointerMoved;
        Chart.GestureRecognizers.Add(gesture);
    }

    private Scatter AddSeries(IReadOnlyList<UsdtPoint> series, ScottPlot.Color color)
    {
        var xs = series.Select(p => p.Time.DateTime).ToArray();
        var ys = series.Select(p => (double)p.Usdt).ToArray();
        var scatter = Chart.Plot.Add.Scatter(xs, ys);
        scatter.MarkerSize = 0;
        scatter.LineWidth = 2;
        scatter.Color = color;
        return scatter;
    }

    private void OnLegendSeed42Tapped(object? sender, TappedEventArgs e) =>
        OnLegendTapped(ChartLegendId.Seed42);

    private void OnLegendSeed7Tapped(object? sender, TappedEventArgs e) =>
        OnLegendTapped(ChartLegendId.Seed7);

    private void OnLegendSumTapped(object? sender, TappedEventArgs e) =>
        OnLegendTapped(ChartLegendId.Sum);

    private void OnLegendTapped(ChartLegendId id)
    {
        if (!_legendSelection.TryToggle(id))
            return;

        ApplyLegendState();
        Chart.Refresh();
    }

    private void ApplyLegendState()
    {
        var plotted = _legendSelection.Plotted;

        foreach (var (id, scatter) in _scatters)
            scatter.IsVisible = plotted.Contains(id);

        UpdateLegendOpacity(LegendSeed42, ChartLegendId.Seed42);
        UpdateLegendOpacity(LegendSeed7, ChartLegendId.Seed7);
        UpdateLegendOpacity(LegendSum, ChartLegendId.Sum);

        ApplyCustomTicks();
        Chart.Plot.Axes.AutoScale();
    }

    private void UpdateLegendOpacity(VisualElement legendItem, ChartLegendId id)
    {
        legendItem.Opacity = _legendSelection.IsEnabled(id) ? 1.0 : HiddenLegendOpacity;
    }

    private void ApplyCustomTicks()
    {
        var xTicks = TickQuantizer.MajorX(_seed42Series[0].Time, _seed42Series[^1].Time, maxTickCount: 12);
        var xManual = new DateTimeManual();
        foreach (var tick in xTicks)
            xManual.AddMajor(tick.DateTime, tick.ToString("yyyy-MM-dd"));
        Chart.Plot.Axes.Bottom.TickGenerator = xManual;

        var plottedPoints = GetPlottedPoints();
        var yMin = plottedPoints.Min(p => p.Usdt);
        var yMax = plottedPoints.Max(p => p.Usdt);
        var yTicks = TickQuantizer.MajorY(yMin, yMax, maxTickCount: 8);
        var yManual = new NumericManual();
        foreach (var tick in yTicks)
            yManual.AddMajor(tick, tick.ToString());
        Chart.Plot.Axes.Left.TickGenerator = yManual;
    }

    private IEnumerable<UsdtPoint> GetPlottedPoints()
    {
        foreach (var id in _legendSelection.Plotted)
        {
            foreach (var point in GetSeries(id))
                yield return point;
        }
    }

    private IReadOnlyList<UsdtPoint> GetSeries(ChartLegendId id) => id switch
    {
        ChartLegendId.Seed42 => _seed42Series,
        ChartLegendId.Seed7 => _seed7Series,
        ChartLegendId.Sum => _sumSeries,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_crosshair is null)
            return;

        var position = e.GetPosition(Chart);
        if (position is null)
            return;

        var coords = Chart.Plot.GetCoordinates((float)position.Value.X, (float)position.Value.Y);
        var x = new DateTimeOffset(DateTime.FromOADate(coords.X), UtcMinusFiveUsdtSeries.Offset);
        var plotted = _legendSelection.Plotted;
        if (plotted.Count == 0)
            return;

        var labels = plotted
            .Select(id => LineHover.Format(LineHover.NearestByX(GetSeries(id), x)))
            .ToArray();

        HoverLabel.Text = string.Join(" | ", labels);

        var anchor = LineHover.NearestByX(GetSeries(plotted[0]), x);
        _crosshair.IsVisible = true;
        _crosshair.Position = new Coordinates(anchor.Time.DateTime.ToOADate(), anchor.Usdt);
        Chart.Refresh();
    }
}
