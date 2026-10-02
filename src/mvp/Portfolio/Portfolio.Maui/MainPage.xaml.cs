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

    private static readonly ScottPlot.Color Seed42Color = ScottPlot.Color.FromHex("#1E88E5");

    private static readonly ScottPlot.Color Seed7Color = ScottPlot.Color.FromHex("#FB8C00");

    private static readonly ScottPlot.Color Seed13Color = ScottPlot.Color.FromHex("#8E24AA");

    private static readonly ScottPlot.Color SumColor = ScottPlot.Color.FromHex("#43A047");

    private static float _displayDensity = 1;

    private ReturnScenario _scenario = ReturnScenario.PositiveBias;

    private ChartLegendSelection _legendSelection = new();

    private Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>> _walkSeries = new();

    private IReadOnlyList<UsdtPoint> _sumSeries = [];

    private Dictionary<(ChartLegendId First, ChartLegendId Second), IReadOnlyList<CorrelationPoint>> _correlationPairs = new();

    private readonly Dictionary<ChartLegendId, Scatter> _scatters = new();

    private Plot? _correlationPlot;

    private Scatter? _correlationScatter;

    private Crosshair? _crosshair;

    private Crosshair? _correlationCrosshair;

    private bool _axisAlignmentRefreshInProgress;

    public MainPage()

    {

        InitializeComponent();

        LoadScenario(ReturnScenario.PositiveBias);

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

    private void LoadScenario(ReturnScenario scenario)

    {

        if (_scenario == scenario && _walkSeries.Count > 0)

            return;

        _scenario = scenario;

        var drift = ReturnScenarioSeries.Drift(scenario);

        var walkIds = ReturnScenarioSeries.Seeds(scenario).Select(LegendIdForSeed).ToArray();

        _walkSeries = new Dictionary<ChartLegendId, IReadOnlyList<UsdtPoint>>();

        foreach (var seed in ReturnScenarioSeries.Seeds(scenario))

        {

            var id = LegendIdForSeed(seed);

            _walkSeries[id] = UtcMinusFiveUsdtSeries.Generate(seed, drift);

        }

        _sumSeries = SumWalks(_walkSeries.Values);

        BuildCorrelationPairs();

        _legendSelection = new ChartLegendSelection(walkIds);

        LegendSeed13.IsVisible = scenario == ReturnScenario.PositiveBias;

        UpdateScenarioSwitcherOpacity();

        HoverLabel.Text = "";

        if (_crosshair is not null)

        {

            RefreshChartSeriesData();

            RemoveCorrelationPanel();

            ApplyLegendState();

            Chart.Refresh();

        }

    }

    private static ChartLegendId LegendIdForSeed(int seed) => seed switch

    {

        UtcMinusFiveUsdtSeries.DefaultSeed => ChartLegendId.Seed42,

        UtcMinusFiveUsdtSeries.SecondSeed => ChartLegendId.Seed7,

        UtcMinusFiveUsdtSeries.ThirdSeed => ChartLegendId.Seed13,

        _ => throw new ArgumentOutOfRangeException(nameof(seed))

    };

    private static IReadOnlyList<UsdtPoint> SumWalks(IEnumerable<IReadOnlyList<UsdtPoint>> walks)

    {

        IReadOnlyList<UsdtPoint>? sum = null;

        foreach (var walk in walks)

            sum = sum is null ? walk : UtcMinusFiveUsdtSeries.Sum(sum, walk);

        return sum ?? throw new InvalidOperationException("At least one walk is required.");

    }

    private void BuildCorrelationPairs()

    {

        _correlationPairs.Clear();

        var ids = _walkSeries.Keys.OrderBy(id => id).ToArray();

        for (var i = 0; i < ids.Length; i++)

        {

            for (var j = i + 1; j < ids.Length; j++)

            {

                var key = (ids[i], ids[j]);

                _correlationPairs[key] = SeriesCorrelation.RollingLogReturnPearson(

                    _walkSeries[ids[i]],

                    _walkSeries[ids[j]]);

            }

        }

    }

    private void BuildChart()

    {

        ConfigureDevicePixelRendering(Chart, Chart.Plot);

        ReplaceSeries(ChartLegendId.Seed42, _walkSeries[ChartLegendId.Seed42], Seed42Color);

        ReplaceSeries(ChartLegendId.Seed7, _walkSeries[ChartLegendId.Seed7], Seed7Color);

        if (_walkSeries.TryGetValue(ChartLegendId.Seed13, out var seed13))

            ReplaceSeries(ChartLegendId.Seed13, seed13, Seed13Color);

        ReplaceSeries(ChartLegendId.Sum, _sumSeries, SumColor);

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

    private void RefreshChartSeriesData()

    {

        ReplaceSeries(ChartLegendId.Seed42, _walkSeries[ChartLegendId.Seed42], Seed42Color);

        ReplaceSeries(ChartLegendId.Seed7, _walkSeries[ChartLegendId.Seed7], Seed7Color);

        if (_walkSeries.TryGetValue(ChartLegendId.Seed13, out var seed13))

            ReplaceSeries(ChartLegendId.Seed13, seed13, Seed13Color);

        else if (_scatters.Remove(ChartLegendId.Seed13, out var removed))

            Chart.Plot.Remove(removed);

        ReplaceSeries(ChartLegendId.Sum, _sumSeries, SumColor);

    }

    private void ReplaceSeries(ChartLegendId id, IReadOnlyList<UsdtPoint> series, ScottPlot.Color color)

    {

        if (_scatters.TryGetValue(id, out var existing))

            Chart.Plot.Remove(existing);

        _scatters[id] = AddSeries(Chart.Plot, series, color);

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

    private void OnScenarioNegativeGrowthTapped(object? sender, TappedEventArgs e) =>

        LoadScenario(ReturnScenario.NegativeGrowth);

    private void OnScenarioPositiveBiasTapped(object? sender, TappedEventArgs e) =>

        LoadScenario(ReturnScenario.PositiveBias);

    private void OnLegendSeed42Tapped(object? sender, TappedEventArgs e) =>

        OnLegendTapped(ChartLegendId.Seed42);

    private void OnLegendSeed7Tapped(object? sender, TappedEventArgs e) =>

        OnLegendTapped(ChartLegendId.Seed7);

    private void OnLegendSeed13Tapped(object? sender, TappedEventArgs e) =>

        OnLegendTapped(ChartLegendId.Seed13);

    private void OnLegendSumTapped(object? sender, TappedEventArgs e) =>

        OnLegendTapped(ChartLegendId.Sum);

    private void OnLegendTapped(ChartLegendId id)

    {

        if (!_legendSelection.TryToggle(id))

            return;

        ApplyLegendState();

        Chart.Refresh();

    }

    private void UpdateScenarioSwitcherOpacity()

    {

        ScenarioNegativeGrowth.Opacity = _scenario == ReturnScenario.NegativeGrowth ? 1.0 : HiddenLegendOpacity;

        ScenarioPositiveBias.Opacity = _scenario == ReturnScenario.PositiveBias ? 1.0 : HiddenLegendOpacity;

    }

    private void ApplyLegendState()

    {

        var plotted = _legendSelection.Plotted;

        foreach (var (id, scatter) in _scatters)

            scatter.IsVisible = plotted.Contains(id);

        UpdateLegendOpacity(LegendSeed42, ChartLegendId.Seed42);

        UpdateLegendOpacity(LegendSeed7, ChartLegendId.Seed7);

        if (LegendSeed13.IsVisible)

            UpdateLegendOpacity(LegendSeed13, ChartLegendId.Seed13);

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

            _correlationPlot.Axes.SetLimitsY(-1, 1);

        ResetMultiplotAxisPanelSizes();

    }

    private IReadOnlyList<CorrelationPoint> GetActiveCorrelationSeries()

    {

        if (!_legendSelection.ShowCorrelation)

            return [];

        var walks = _legendSelection.Plotted.Where(id => id != ChartLegendId.Sum).ToArray();

        if (walks.Length != 2)

            return [];

        var first = walks[0];

        var second = walks[1];

        if (first > second)

            (first, second) = (second, first);

        return _correlationPairs.TryGetValue((first, second), out var series) ? series : [];

    }

    private void EnsureCorrelationPanel()

    {

        var correlationSeries = GetActiveCorrelationSeries();

        if (_correlationPlot is not null)

        {

            if (_correlationScatter is not null)

            {

                _correlationPlot.Remove(_correlationScatter);

                _correlationScatter = AddCorrelationSeries(_correlationPlot, correlationSeries);

                _correlationScatter.IsVisible = true;

            }

            Chart.Plot.Axes.Bottom.IsVisible = false;

            _correlationPlot.Axes.Bottom.IsVisible = true;

            return;

        }

        _correlationPlot = Chart.Multiplot.AddPlot();

        ConfigureDevicePixelRendering(Chart, _correlationPlot);

        _correlationScatter = AddCorrelationSeries(_correlationPlot, correlationSeries);

        _correlationPlot.Axes.Bottom.Label.Text = "UTC-5";

        _correlationPlot.Axes.Left.Label.Text = "Correlation";

        _correlationPlot.Axes.SetLimitsY(-1, 1);

        Chart.Multiplot.Layout = CorrelationLayout;

        Chart.Multiplot.SharedAxes.ShareX([Chart.Plot, _correlationPlot]);

        Chart.Multiplot.CollapseVertically();

        Chart.Plot.Axes.Bottom.IsVisible = false;

        _correlationPlot.Axes.Bottom.IsVisible = true;

        _correlationCrosshair = _correlationPlot.Add.Crosshair(0, 0);

        _correlationCrosshair.IsVisible = false;

        AttachGrowingAxisAlignment();

    }

    private void RemoveCorrelationPanel()

    {

        if (_correlationPlot is null)

        {

            Chart.Plot.Axes.Bottom.IsVisible = true;

            return;

        }

        DetachGrowingAxisAlignment();

        Chart.Multiplot.RemovePlot(_correlationPlot);

        Chart.Multiplot.SharedAxes.ShareX([]);

        Chart.Multiplot.Layout = new Rows();

        _correlationPlot = null;

        _correlationScatter = null;

        _correlationCrosshair = null;

        Chart.Plot.Axes.Bottom.IsVisible = true;

        Chart.Plot.Axes.Bottom.ResetSize();

        ResetMultiplotAxisPanelSizes();

    }

    private void AttachGrowingAxisAlignment()

    {

        Chart.Plot.RenderManager.RenderFinished += OnSubplotRenderFinished;

        if (_correlationPlot is not null)

            _correlationPlot.RenderManager.RenderFinished += OnSubplotRenderFinished;

    }

    private void DetachGrowingAxisAlignment()

    {

        UnsubscribeRenderFinished(Chart.Plot);

        if (_correlationPlot is not null)

            UnsubscribeRenderFinished(_correlationPlot);

    }

    private void UnsubscribeRenderFinished(Plot plot)

    {

        var remaining = Delegate.Remove(

            plot.RenderManager.RenderFinished,

            (EventHandler<RenderDetails>)OnSubplotRenderFinished);

        plot.RenderManager.RenderFinished = remaining as EventHandler<RenderDetails> ?? delegate { };

    }

    private void OnSubplotRenderFinished(object? sender, RenderDetails rd)

    {

        if (_correlationPlot is null || sender is not Plot sourcePlot)

            return;

        var leftSize = rd.Layout.PanelSizes[sourcePlot.Axes.Left];

        var rightSize = rd.Layout.PanelSizes[sourcePlot.Axes.Right];

        var plots = Chart.Multiplot.GetPlots();

        var targetLeft = plots.Max(p => Math.Max(p.Axes.Left.MinimumSize, leftSize));

        var targetRight = plots.Max(p => Math.Max(p.Axes.Right.MinimumSize, rightSize));

        var changed = false;

        foreach (var plot in plots)

        {

            if (plot.Axes.Left.MinimumSize < targetLeft)

            {

                plot.Axes.Left.MinimumSize = targetLeft;

                changed = true;

            }

            if (plot.Axes.Right.MinimumSize < targetRight)

            {

                plot.Axes.Right.MinimumSize = targetRight;

                changed = true;

            }

        }

        if (!changed || _axisAlignmentRefreshInProgress)

            return;

        _axisAlignmentRefreshInProgress = true;

        Chart.Refresh();

        _axisAlignmentRefreshInProgress = false;

    }

    private void ResetMultiplotAxisPanelSizes()

    {

        foreach (var plot in Chart.Multiplot.GetPlots())

        {

            plot.Axes.Left.ResetSize();

            plot.Axes.Right.ResetSize();

        }

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

        var anchor = _walkSeries.Values.First();

        var xTicks = TickQuantizer.MajorX(anchor[0].Time, anchor[^1].Time, maxTickCount: 12);

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

        ChartLegendId.Seed42 => _walkSeries[ChartLegendId.Seed42],

        ChartLegendId.Seed7 => _walkSeries[ChartLegendId.Seed7],

        ChartLegendId.Seed13 => _walkSeries[ChartLegendId.Seed13],

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

        var pixel = new Pixel(

            (float)(position.Value.X * density),

            (float)(position.Value.Y * density));

        var plotUnderMouse = Chart.Multiplot.GetPlotAtPixel(pixel) ?? Chart.Plot;

        var coords = plotUnderMouse.GetCoordinates(pixel);

        var x = new DateTimeOffset(DateTime.FromOADate(coords.X), UtcMinusFiveUsdtSeries.Offset);

        var plotted = _legendSelection.Plotted;

        if (plotted.Count == 0)

            return;

        var labels = plotted

            .Select(id => LineHover.Format(LineHover.NearestByX(GetSeries(id), x)))

            .ToArray();

        var hoverText = string.Join(" | ", labels);

        var correlationSeries = GetActiveCorrelationSeries();

        if (_legendSelection.ShowCorrelation && correlationSeries.Count > 0)

        {

            var nearestCorrelation = NearestCorrelationByX(correlationSeries, x);

            hoverText += $" | r = {nearestCorrelation.Correlation:F3}";

        }

        HoverLabel.Text = hoverText;

        var anchor = LineHover.NearestByX(GetSeries(plotted[0]), x);

        var snappedX = anchor.Time.DateTime.ToOADate();

        _crosshair.IsVisible = true;

        var showCorrelationPanel = _correlationCrosshair is not null && _legendSelection.ShowCorrelation;

        if (!showCorrelationPanel)

        {

            _crosshair.HorizontalLine.IsVisible = true;

            _crosshair.VerticalLine.IsVisible = true;

            _crosshair.Position = new Coordinates(snappedX, anchor.Usdt);

            Chart.Refresh();

            return;

        }

        _crosshair.VerticalLine.IsVisible = true;

        _correlationCrosshair!.VerticalLine.IsVisible = true;

        if (plotUnderMouse == _correlationPlot)

        {

            var nearestCorrelation = NearestCorrelationByX(correlationSeries, anchor.Time);

            _correlationCrosshair.HorizontalLine.IsVisible = true;

            _correlationCrosshair.Position = new Coordinates(snappedX, nearestCorrelation.Correlation);

            _crosshair.HorizontalLine.IsVisible = false;

            _crosshair.Position = new Coordinates(snappedX, anchor.Usdt);

        }

        else

        {

            _crosshair.HorizontalLine.IsVisible = true;

            _crosshair.Position = new Coordinates(snappedX, anchor.Usdt);

            var nearestCorrelation = NearestCorrelationByX(correlationSeries, anchor.Time);

            _correlationCrosshair.HorizontalLine.IsVisible = false;

            _correlationCrosshair.Position = new Coordinates(snappedX, nearestCorrelation.Correlation);

        }

        _correlationCrosshair.IsVisible = true;

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

