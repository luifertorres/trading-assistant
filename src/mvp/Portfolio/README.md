# Portfolio MVP (MAUI chart)

Isolated **MAUI** app that plots a daily USDT line chart. This is an MVP under `src/mvp/Portfolio/` — **not** the Platform `Portfolio` bounded context.

**Charting:** [ScottPlot.Maui](https://scottplot.net/) (MIT). [Skender.Stock.Indicators](https://dotnet.stockindicators.dev/) is calculation-only and has no charts package.

**Solution:** [`Portfolio.slnx`](Portfolio.slnx)

## Projects

| Project | Role |
|---------|------|
| [`Portfolio/`](Portfolio/) | net10.0 lib: series, tick quantizer, hover snap/format |
| [`Portfolio.Tests/`](Portfolio.Tests/) | xUnit + FluentAssertions (TDD) |
| [`Portfolio.Maui/`](Portfolio.Maui/) | Windows MAUI host (`ApplicationTitle` = Portfolio) |

## Chart rules

- **X:** UTC-5 midnight, one point per day from `2020-08-01` through `2026-09-19` inclusive.
- **Y:** two random walks — each starts in `3000–5000` USDT (seeds `42` and `7`); each next day changes by at most `previous × 0.01 × random(−1..1)` (±1% of the prior day). A third series is the day-by-day sum.
- **Legends:** three tappable items under the chart — `Seed 42` (blue), `Seed 7` (orange), `Sum` (green). **Sum on** plots only the summed line. **Sum off** plots each walk whose flag is on. Tapping a walk while Sum is on flips that walk’s flag for when Sum is turned off. Tapping a walk while Sum is off shows or hides that line; the last visible line cannot be turned off. Disabled flags render at lower opacity but stay tappable.
- **Correlation panel:** a secondary chart below the main USDT chart plots the **rolling 30-day Pearson correlation of daily log returns** between the visible individual walks (today Seed 42 vs Seed 7). Y axis is fixed at −1…1. The panel is shown only when **more than one** individual series is plotted; it stays hidden while **Sum** is shown or when only one walk is visible. Hover appends `r = …` (three decimals) when the panel is visible.
- **Axis quantum:** 1 day (X), 1 USDT (Y); ticks stride by whole days / whole USDT when dense. Y autoscale follows the plotted series only.
- **Hover:** nearest sample by X on each plotted series (no interpolated Y); one series shows UTC-5 timestamp and USDT, two individual walks join both with ` | `.

## Build / test

From repo root:

```bash
dotnet build src/mvp/Portfolio/Portfolio.Maui/Portfolio.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet test src/mvp/Portfolio/Portfolio.Tests/Portfolio.Tests.csproj
```

Requires the MAUI Windows workload (`dotnet workload install maui-windows` if missing).

## Run

```bash
dotnet run --project src/mvp/Portfolio/Portfolio.Maui/Portfolio.Maui.csproj -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None
```

If the window closes immediately, confirm `WindowsPackageType` is `None` in the csproj (unpackaged WinUI) and that the MAUI Windows workload is installed.

Move the pointer along the line to see X/Y values in the label below the legends. Tap legends to switch between the sum and individual walks.
