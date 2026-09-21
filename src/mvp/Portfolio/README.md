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
- **Y:** random walk — first day in `2000–5000` USDT; each next day changes by `previous × 0.05 × random(−1..1)` (±5% of the prior day).
- **Axis quantum:** 1 day (X), 1 USDT (Y); ticks stride by whole days / whole USDT when dense.
- **Hover:** nearest sample by X (no interpolated Y); label shows UTC-5 timestamp and USDT.

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

Move the pointer along the line to see X/Y values in the label below the chart.
