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

- **Return scenarios** (tappable row under the chart; app opens on **Positive bias**):

  - **Negative growth:** seeds `42` and `7`, zero daily drift — symmetric ±1% shock only (long-run log growth tends negative).

  - **Positive bias:** seeds `42`, `7`, and `13`, daily drift `0.000236` on top of the same ±1% shock (long-run log growth ~8%/year). Independent walks — the **null baseline** for dependence readouts.

  - **Shared factor:** same seeds and drift; daily shock `μ + 0.01 × (√0.6 × m_t + √0.4 × e_i,t)` with one market draw `m_t` per day (`MarketSeed = 2023`) and per-walk `e_i,t` from each seed’s RNG (same draw order as Positive bias).

  - **Crash-coupled:** with probability `0.01` per day (market stream), all walks take `−2.5%` that day; otherwise `μ′ + 0.01 × e_i,t` with `μ′ = (μ + 0.01 × 0.025) / 0.99` so long-run drift matches Positive bias.

  - **Complementary regimes:** regime flips with probability `1/120` per day; seed `42` gets `±0.001` regime drift (sign flips with regime), seed `7` the opposite sign, seed `13` no regime term; all still add `μ + 0.01 × e_i,t`.

- **Y:** each walk starts in `3000–5000` USDT; each next day uses `previous × (1 + drift + 0.01 × random(−1..1))` (integer USDT) unless a dependence scenario overrides the rate formula above. **Sum** is the day-by-day sum of that scenario’s walks only.

- **Dependence readout** (Consolas label under hover): one row per pair (`42×7`, `42×13`, `7×13`) plus a portfolio summary line.

  - **Theory 1 — `rho1d` / `rho30d`:** Pearson correlation of daily and 30-day non-overlapping log returns (Scherman’s stated PnL correlation).

  - **Theory 2 — `tail5%`:** share of A’s worst 5% return days that are also among B’s worst 5% (~`0.05` if independent).

  - **Theory 3 — `uwRho`:** Pearson correlation of underwater (drawdown-from-peak) fractions (Platform ADR-005).

  - **Theory 4 — `lift-10%`:** co-underwater lift at −10% depth, `P(both) / (P(A) × P(B))` (`n/a` if either side never breaches).

  - **Theory 5 — `7|42worst3` / `42|7worst3`:** average simple return of one walk over the other’s three deepest drawdown episodes.

  - **Theory 6 — portfolio line:** equal-weight daily-rebalanced index (10 000 USDT start) vs member-average **max drawdown**, **Ulcer**, **CDaR95**, and **longest underwater** days.

- **Legends:** `Seed 42` (blue), `Seed 7` (orange), `Seed 13` (purple, Positive bias only), `Sum` (green). **Sum on** plots only the summed line. **Sum off** plots each walk whose flag is on. Tapping a walk while Sum is on flips that walk’s flag for when Sum is turned off. Tapping a walk while Sum is off shows or hides that line; the last visible line cannot be turned off. Disabled flags render at lower opacity but stay tappable. Switching scenario rebuilds the series and resets legends to Sum on.

- **Drawdown panel:** each plotted walk’s **drawdown from its own historical peak** (fraction below running max USDT). Shown when **Sum** is off and **two or more** individual walks are visible (including all three in Positive bias). Hidden while **Sum** is plotted or only one walk remains. One colored line per walk; Y axis tops at 0. Hover appends seed drawdowns (e.g. `42 -3.2% | 7 0.0%`).

- **Axis quantum:** 1 day (X), 1 USDT (Y); ticks stride by whole days / whole USDT when dense. Y autoscale follows the plotted series only.

- **Hover:** nearest sample by X on each plotted series (no interpolated Y); one series shows UTC-5 timestamp and USDT, multiple individual walks join with ` | `.



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



Move the pointer along the line to see X/Y values in the label below the legends. Tap scenarios to compare independent vs engineered dependence; tap legends to switch between the sum and individual walks and to open the drawdown panel.


