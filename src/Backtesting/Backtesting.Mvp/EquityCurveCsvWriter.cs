using System.Globalization;

namespace Backtesting.Mvp;

public static class EquityCurveCsvWriter
{
    public static void Write(string path, IReadOnlyList<EquityPoint> curve)
    {
        var inv = CultureInfo.InvariantCulture;
        using var w = new StreamWriter(path);
        w.WriteLine("Time,Equity");
        foreach (var p in curve)
            w.WriteLine($"{p.Time:O},{p.Equity.ToString(inv)}");
    }
}
