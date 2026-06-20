using System;
using System.Collections.Generic;

namespace TradingAssistant;

internal static class SymbolExclusions
{
    private static readonly HashSet<string> s_blockedSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BABYUSDT",
    };

    public static bool Contains(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        return s_blockedSymbols.Contains(symbol);
    }
}


