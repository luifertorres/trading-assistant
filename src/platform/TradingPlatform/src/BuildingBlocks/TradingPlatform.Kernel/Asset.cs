namespace TradingPlatform.Kernel;

/// <summary>Canonical broker identity: broker:venue:symbol (e.g. binance:usdm:BTCUSDT).</summary>
public readonly record struct Asset(string Value)
{
    public static Asset FromUsdmExchangeSymbol(string exchangeSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeSymbol);
        return new Asset($"binance:usdm:{exchangeSymbol.Trim()}");
    }

    public static Asset Parse(string value)
    {
        if (!TryParse(value, out var asset))
            throw new FormatException($"Invalid asset format: '{value}'. Expected broker:venue:symbol.");

        return asset;
    }

    public static bool TryParse(string? value, out Asset asset)
    {
        asset = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (string.IsNullOrWhiteSpace(parts[0])
            || string.IsNullOrWhiteSpace(parts[1])
            || string.IsNullOrWhiteSpace(parts[2]))
            return false;

        asset = new Asset($"{parts[0]}:{parts[1]}:{parts[2]}");
        return true;
    }

    public override string ToString() => Value;
}
