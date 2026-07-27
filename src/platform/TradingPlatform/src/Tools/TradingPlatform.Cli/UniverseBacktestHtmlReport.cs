using System.Globalization;
using System.Net;
using System.Text;
using Research.Application;
using TradingPlatform.Kernel;

namespace TradingPlatform.Cli;

internal sealed record UniverseBacktestRow(
    string Asset,
    string Symbol,
    Direction Direction,
    int TradeCount,
    decimal ReturnFraction,
    decimal MaxDrawdownFraction,
    decimal ProfitFactor,
    bool Pass,
    string? FailReason);

internal static class UniverseBacktestHtmlReport
{
    public static string Render(
        UniverseBacktestArgs args,
        int instrumentCount,
        int vectorCount,
        IReadOnlyList<UniverseBacktestRow> rows)
    {
        var passCount = rows.Count(r => r.Pass);
        var failCount = rows.Count - passCount;
        var generatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head>");
        sb.AppendLine("<meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine("<title>Universe backtest — ").Append(H(args.TradingLogic)).AppendLine("</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("""
            :root { --bg: #0f1419; --card: #1a2332; --text: #e7ecf3; --muted: #8b9bb4; --pass: #1f6f4a; --fail: #7a2e2e; --border: #2d3a4f; }
            * { box-sizing: border-box; }
            body { font-family: system-ui, Segoe UI, sans-serif; background: var(--bg); color: var(--text); margin: 0; padding: 1.5rem; }
            h1 { font-size: 1.35rem; margin: 0 0 0.5rem; }
            .meta { color: var(--muted); font-size: 0.9rem; margin-bottom: 1rem; }
            .summary { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-bottom: 1rem; }
            .chip { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 0.5rem 0.85rem; font-size: 0.9rem; }
            .chip strong { display: block; font-size: 1.1rem; }
            .filters { margin-bottom: 0.75rem; }
            .filters button { background: var(--card); color: var(--text); border: 1px solid var(--border); border-radius: 6px; padding: 0.35rem 0.75rem; margin-right: 0.35rem; cursor: pointer; }
            .filters button.active { border-color: #5b8def; }
            .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: 8px; max-height: 75vh; }
            table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
            th, td { padding: 0.45rem 0.6rem; text-align: left; border-bottom: 1px solid var(--border); white-space: nowrap; }
            th { position: sticky; top: 0; background: #243044; z-index: 1; cursor: pointer; user-select: none; }
            th[data-sort]::after { content: ' ⇅'; color: var(--muted); font-size: 0.75em; }
            th[data-sort="asc"]::after { content: ' ▲'; color: #5b8def; }
            th[data-sort="desc"]::after { content: ' ▼'; color: #5b8def; }
            tr.pass td { background: rgba(31, 111, 74, 0.15); }
            tr.fail td { background: rgba(122, 46, 46, 0.12); }
            tr:hover td { filter: brightness(1.08); }
            .num { text-align: right; font-variant-numeric: tabular-nums; }
            .reason { max-width: 28rem; white-space: normal; color: var(--muted); font-size: 0.8rem; }
            """);
        sb.AppendLine("</style></head><body>");

        sb.Append("<h1>Universe backtest — ").Append(H(args.TradingLogic)).AppendLine("</h1>");
        sb.Append("<p class=\"meta\">Generated ").Append(H(generatedAt));
        sb.Append(" · TF 1D · capital ").Append(args.InitialCapital.ToString(CultureInfo.InvariantCulture));
        sb.Append(" · vector-risk ").Append(args.VectorRiskFraction.ToString(CultureInfo.InvariantCulture));
        sb.Append(" · fee ").Append(args.FeeBpsPerSide.ToString(CultureInfo.InvariantCulture)).AppendLine(" bps</p>");

        sb.AppendLine("<div class=\"summary\">");
        AppendChip(sb, "Instruments", instrumentCount.ToString(CultureInfo.InvariantCulture));
        AppendChip(sb, "Vectors", vectorCount.ToString(CultureInfo.InvariantCulture));
        AppendChip(sb, "PASS", passCount.ToString(CultureInfo.InvariantCulture));
        AppendChip(sb, "FAIL", failCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("</div>");

        sb.AppendLine("""
            <div class="filters">
              <button type="button" class="active" data-filter="all">All</button>
              <button type="button" data-filter="pass">PASS</button>
              <button type="button" data-filter="fail">FAIL</button>
            </div>
            """);

        sb.AppendLine("<div class=\"table-wrap\"><table id=\"results\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th data-col=\"0\">Asset</th><th data-col=\"1\">Symbol</th><th data-col=\"2\">Direction</th>");
        sb.AppendLine("<th data-col=\"3\" class=\"num\">Trades</th><th data-col=\"4\" class=\"num\">Return</th>");
        sb.AppendLine("<th data-col=\"5\" class=\"num\">MaxDD</th><th data-col=\"6\" class=\"num\">PF</th>");
        sb.AppendLine("<th data-col=\"7\">Verdict</th><th>Fail reason</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var row in rows)
        {
            var verdictClass = row.Pass ? "pass" : "fail";
            var label = row.Pass ? "PASS" : "FAIL";
            sb.Append("<tr class=\"").Append(verdictClass).Append("\" data-verdict=\"").Append(label.ToLowerInvariant()).AppendLine("\">");
            sb.Append("<td>").Append(H(row.Asset)).Append("</td>");
            sb.Append("<td>").Append(H(row.Symbol)).Append("</td>");
            sb.Append("<td>").Append(H(row.Direction.ToString())).Append("</td>");
            sb.Append("<td class=\"num\">").Append(row.TradeCount.ToString(CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td class=\"num\" data-value=\"").Append(row.ReturnFraction.ToString(CultureInfo.InvariantCulture)).Append("\">")
                .Append(row.ReturnFraction.ToString("P2", CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td class=\"num\" data-value=\"").Append(row.MaxDrawdownFraction.ToString(CultureInfo.InvariantCulture)).Append("\">")
                .Append(row.MaxDrawdownFraction.ToString("P2", CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td class=\"num\" data-value=\"").Append(row.ProfitFactor.ToString(CultureInfo.InvariantCulture)).Append("\" title=\"")
                .Append(H(FormatProfitFactorTooltip(row.ProfitFactor))).Append("\">")
                .Append(FormatProfitFactor(row.ProfitFactor)).Append("</td>");
            sb.Append("<td>").Append(label).Append("</td>");
            sb.Append("<td class=\"reason\">").Append(H(row.FailReason ?? "—")).AppendLine("</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></div>");
        sb.AppendLine("""
            <script>
            document.querySelectorAll('.filters button').forEach(btn => {
              btn.addEventListener('click', () => {
                document.querySelectorAll('.filters button').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                const f = btn.dataset.filter;
                document.querySelectorAll('#results tbody tr').forEach(tr => {
                  tr.style.display = (f === 'all' || tr.dataset.verdict === f) ? '' : 'none';
                });
              });
            });
            document.querySelectorAll('#results th[data-col]').forEach(th => {
              th.addEventListener('click', () => {
                const table = document.getElementById('results');
                const col = +th.dataset.col;
                const tbody = table.tBodies[0];
                const asc = th.dataset.sort !== 'asc';
                document.querySelectorAll('#results th[data-col]').forEach(h => {
                  if (h !== th) delete h.dataset.sort;
                });
                th.dataset.sort = asc ? 'asc' : 'desc';
                const rows = [...tbody.rows];
                rows.sort((a, b) => {
                  const av = cellSortValue(a.cells[col]);
                  const bv = cellSortValue(b.cells[col]);
                  if (av < bv) return asc ? -1 : 1;
                  if (av > bv) return asc ? 1 : -1;
                  return 0;
                });
                rows.forEach(r => tbody.appendChild(r));
              });
            });
            function cellSortValue(cell) {
              const raw = cell.dataset.value ?? cell.textContent.trim();
              const n = parseFloat(String(raw).replace(/[%,+]/g, ''));
              return Number.isFinite(n) ? n : String(raw).toLowerCase();
            }
            </script>
            """);
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static async Task WriteAsync(string path, string html, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static void AppendChip(StringBuilder sb, string label, string value)
    {
        sb.Append("<div class=\"chip\"><span>").Append(H(label)).Append("</span><strong>")
            .Append(H(value)).AppendLine("</strong></div>");
    }

    private static string FormatProfitFactor(decimal pf) =>
        pf >= 999m ? "∞" : pf.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatProfitFactorTooltip(decimal pf) =>
        pf >= 999m
            ? "No losing trades in sample (PF capped at 999 in engine). Sort uses 999."
            : pf.ToString("F4", CultureInfo.InvariantCulture);

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
