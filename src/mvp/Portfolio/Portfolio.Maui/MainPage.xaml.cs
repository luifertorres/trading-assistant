using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;

namespace Portfolio.Maui;

public partial class MainPage : ContentPage
{
    private readonly IReadOnlyList<UsdtPoint> _series;
    private Crosshair? _crosshair;

    public MainPage()
    {
        InitializeComponent();
        _series = UtcMinusFiveUsdtSeries.Generate();
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
        var xs = _series.Select(p => p.Time.DateTime).ToArray();
        var ys = _series.Select(p => (double)p.Usdt).ToArray();

        var scatter = Chart.Plot.Add.Scatter(xs, ys);
        scatter.MarkerSize = 0;
        scatter.LineWidth = 2;

        Chart.Plot.Axes.Bottom.Label.Text = "UTC-5";
        Chart.Plot.Axes.Left.Label.Text = "USDT";
        ApplyCustomTicks();
        Chart.Plot.Axes.AutoScale();

        _crosshair = Chart.Plot.Add.Crosshair(0, 0);
        _crosshair.IsVisible = false;

        Chart.Refresh();

        var gesture = new PointerGestureRecognizer();
        gesture.PointerMoved += OnPointerMoved;
        Chart.GestureRecognizers.Add(gesture);
    }

    private void ApplyCustomTicks()
    {
        var xTicks = TickQuantizer.MajorX(_series[0].Time, _series[^1].Time, maxTickCount: 12);
        var xManual = new DateTimeManual();
        foreach (var tick in xTicks)
            xManual.AddMajor(tick.DateTime, tick.ToString("yyyy-MM-dd"));
        Chart.Plot.Axes.Bottom.TickGenerator = xManual;

        var yTicks = TickQuantizer.MajorY(
            UtcMinusFiveUsdtSeries.MinUsdt,
            UtcMinusFiveUsdtSeries.MaxUsdt,
            maxTickCount: 8);
        var yManual = new NumericManual();
        foreach (var tick in yTicks)
            yManual.AddMajor(tick, tick.ToString());
        Chart.Plot.Axes.Left.TickGenerator = yManual;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_crosshair is null)
            return;

        var position = e.GetPosition(Chart);
        if (position is null)
            return;

        var coords = Chart.Plot.GetCoordinates((float)position.Value.X, (float)position.Value.Y);
        var x = new DateTimeOffset(DateTime.FromOADate(coords.X), UtcMinusFiveUsdtSeries.Offset);
        var point = LineHover.NearestByX(_series, x);

        HoverLabel.Text = LineHover.Format(point);
        _crosshair.IsVisible = true;
        _crosshair.Position = new Coordinates(point.Time.DateTime.ToOADate(), point.Usdt);
        Chart.Refresh();
    }
}
