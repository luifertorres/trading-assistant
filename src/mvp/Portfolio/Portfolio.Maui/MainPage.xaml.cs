using ScottPlot;
using ScottPlot.MultiplotLayouts;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;

namespace Portfolio.Maui;

public partial class MainPage : ContentPage
{
    private const double HiddenLegendOpacity = 0.4;

    private const float CorrelationPanelHeightLogical = 160;

    private static readonly FixedBottomRowLayout CorrelationLayout = new(CorrelationPanelHeightLogical);

    private static float _displayDensity = 1;

    private readonly IReadOnlyList<UsdtPoint> _seed42Series;
    private readonly IReadOnlyList<UsdtPoint> _seed7Series;
    private readonly IReadOnlyList<UsdtPoint> _sumSeries;
    private readonly IReadOnlyList<CorrelationPoint> _correlationSeries;
    private readonly ChartLegendSelection _legendSelection = new();
    private readonly Dictionary<ChartLegendId, Scatter> _scatters = new();
    private Plot? _correlationPlot;
    private Scatter? _correlationScatter;
    private Crosshair? _crosshair;

    public MainPage()
    {
        InitializeComponent();
        _seed42Series = UtcMinusFiveUsdtSeries.Generate(UtcMinusFiveUsdtSeries.DefaultSeed);
        _seed7Series = UtcMinusFiveUsdtSeries.Generate(UtcMinusFiveUsdtSeries.SecondSeed);
        _sumSeries = UtcMinusFiveUsdtSeries.Sum(_seed42Series, _seed7Series);
        _correlationSeries = SeriesCorrelation.RollingLogReturnPearson(_seed42Series, _seed7Series);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDisplayDensity();
        DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;

        if (_crosshair is not null)
            return;

        BuildChart();
    }

    protected override void OnDisappearing()
    {
        DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
        base.OnDisappearing();
    }

    private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        RefreshDisplayDensity();
        if (_crosshair is null)
            return;

        Chart.Refresh();
    }

    private static void RefreshDisplayDensity()
    {
        var density = (float)DeviceDisplay.MainDisplayInfo.Density;
        _displayDensity = density > 0 ? density : 1;
    }

    private void BuildChart()
    {
        ConfigureDevicePixelRendering(Chart, Chart.Plot);

        _scatters[ChartLegendId.Seed42] = AddSeries(Chart.Plot, _seed42Series, ScottPlot.Colors.Blue);
        _scatters[ChartLegendId.Seed7] = AddSeries(Chart.Plot, _seed7Series, ScottPlot.Colors.Orange);
        _scatters[ChartLegendId.Sum] = AddSeries(Chart.Plot, _sumSeries, ScottPlot.Colors.Green);

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

    private static void ConfigureDevicePixelRendering(ScottPlot.Maui.MauiPlot chart, Plot plot)
    {
        chart.IgnorePixelScaling = false;
        chart.DisplayScale = 1;
        plot.ScaleFactor = 1;
    }

    private static float DisplayDensity() => _displayDensity;

    private static Scatter AddSeries(Plot plot, IReadOnlyList<UsdtPoint> series, ScottPlot.Color color)
    {
        var xs = series.Select(p => p.Time.DateTime).ToArray();
        var ys = series.Select(p => (double)p.Usdt).ToArray();
        var scatter = plot.Add.Scatter(xs, ys);
        scatter.MarkerSize = 0;
        scatter.LineWidth = 2;
        scatter.Color = color;
        return scatter;
    }

    private static Scatter AddCorrelationSeries(Plot plot, IReadOnlyList<CorrelationPoint> series)
    {
        var xs = series.Select(p => p.Time.DateTime).ToArray();
        var ys = series.Select(p => p.Correlation).ToArray();
        var scatter = plot.Add.Scatter(xs, ys);
        scatter.MarkerSize = 0;
        scatter.LineWidth = 2;
        scatter.Color = ScottPlot.Colors.Purple;
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

        var showCorrelation = _legendSelection.ShowCorrelation;
        if (showCorrelation)
            EnsureCorrelationPanel();
        else
            RemoveCorrelationPanel();

        ApplyCustomTicks();
        Chart.Plot.Axes.AutoScale();
        ApplyMainAxisPadding();

        if (showCorrelation && _correlationPlot is not null)
        {
            _correlationPlot.Axes.SetLimitsY(-1, 1);
            _correlationPlot.Axes.AutoScaleX();
        }
    }

    private void EnsureCorrelationPanel()
    {
        if (_correlationPlot is not null)
        {
            if (_correlationScatter is not null)
                _correlationScatter.IsVisible = true;

            Chart.Plot.Axes.Bottom.IsVisible = false;
            _correlationPlot.Axes.Bottom.IsVisible = true;
            return;
        }

        _correlationPlot = Chart.Multiplot.AddPlot();
        ConfigureDevicePixelRendering(Chart, _correlationPlot);
        _correlationScatter = AddCorrelationSeries(_correlationPlot, _correlationSeries);
        _correlationPlot.Axes.Bottom.Label.Text = "UTC-5";
        _correlationPlot.Axes.Left.Label.Text = "Correlation";
        _correlationPlot.Axes.SetLimitsY(-1, 1);

        Chart.Multiplot.Layout = CorrelationLayout;
        Chart.Multiplot.SharedAxes.ShareX([Chart.Plot, _correlationPlot]);
        Chart.Multiplot.CollapseVertically();

        Chart.Plot.Axes.Bottom.IsVisible = false;
        _correlationPlot.Axes.Bottom.IsVisible = true;
    }

    private void RemoveCorrelationPanel()
    {
        if (_correlationPlot is null)
        {
            Chart.Plot.Axes.Bottom.IsVisible = true;
            return;
        }

        Chart.Multiplot.RemovePlot(_correlationPlot);
        Chart.Multiplot.SharedAxes.ShareX([]);
        Chart.Multiplot.Layout = new Rows();

        _correlationPlot = null;
        _correlationScatter = null;

        Chart.Plot.Axes.Bottom.IsVisible = true;
    }

    private void ApplyMainAxisPadding()
    {
        var limits = Chart.Plot.Axes.GetLimits();
        var span = limits.Top - limits.Bottom;
        if (span <= 0)
            return;

        var pad = span * 0.05;
        Chart.Plot.Axes.SetLimitsY(limits.Bottom - pad, limits.Top + pad);
    }

    private sealed class FixedBottomRowLayout(float bottomPlotHeightLogical) : IMultiplotLayout
    {
        public PixelRect[] GetSubplotRectangles(SubplotCollection subplots, PixelRect figureRect)
        {
            var rectangles = new PixelRect[subplots.Count];

            if (subplots.Count == 1)
            {
                rectangles[0] = figureRect;
                return rectangles;
            }

            var density = DisplayDensity();
            var bottomHeight = Math.Min(bottomPlotHeightLogical * density, figureRect.Height * 0.4f);
            var splitY = figureRect.Bottom - bottomHeight;

            rectangles[0] = new PixelRect(figureRect.Left, figureRect.Right, splitY, figureRect.Top);
            rectangles[1] = new PixelRect(figureRect.Left, figureRect.Right, figureRect.Bottom, splitY);

            return rectangles;
        }
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
        if (_correlationPlot is not null)
            _correlationPlot.Axes.Bottom.TickGenerator = xManual;

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

        RefreshDisplayDensity();
        var density = DisplayDensity();
        var coords = Chart.Plot.GetCoordinates(
            (float)(position.Value.X * density),
            (float)(position.Value.Y * density));
        var x = new DateTimeOffset(DateTime.FromOADate(coords.X), UtcMinusFiveUsdtSeries.Offset);
        var plotted = _legendSelection.Plotted;
        if (plotted.Count == 0)
            return;

        var labels = plotted
            .Select(id => LineHover.Format(LineHover.NearestByX(GetSeries(id), x)))
            .ToArray();

        var hoverText = string.Join(" | ", labels);
        if (_legendSelection.ShowCorrelation && _correlationSeries.Count > 0)
        {
            var nearestCorrelation = NearestCorrelationByX(_correlationSeries, x);
            hoverText += $" | r = {nearestCorrelation.Correlation:F3}";
        }

        HoverLabel.Text = hoverText;

        var anchor = LineHover.NearestByX(GetSeries(plotted[0]), x);
        _crosshair.IsVisible = true;
        _crosshair.Position = new Coordinates(anchor.Time.DateTime.ToOADate(), anchor.Usdt);
        Chart.Refresh();
    }

    private static CorrelationPoint NearestCorrelationByX(
        IReadOnlyList<CorrelationPoint> series,
        DateTimeOffset x)
    {
        var nearest = series[0];
        var best = Math.Abs((series[0].Time - x).Ticks);

        foreach (var point in series)
        {
            var distance = Math.Abs((point.Time - x).Ticks);
            if (distance < best)
            {
                best = distance;
                nearest = point;
            }
        }

        return nearest;
    }
}
