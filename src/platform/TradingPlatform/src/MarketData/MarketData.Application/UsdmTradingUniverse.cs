using MarketData.Domain;

namespace MarketData.Application;

/// <summary>Eligible Binance USD-M USDT perpetual trading universe for Scherman vectors.</summary>
public static class UsdmTradingUniverse
{
    private static readonly HashSet<string> ExcludedBaseAssets =
        new(StringComparer.OrdinalIgnoreCase) { "USDT", "USDC" };

    public static bool IsEligible(Instrument instrument) =>
        string.Equals(instrument.Venue, "binance", StringComparison.OrdinalIgnoreCase)
        && string.Equals(instrument.Market, "usdm", StringComparison.OrdinalIgnoreCase)
        && string.Equals(instrument.ContractType, "perpetual", StringComparison.OrdinalIgnoreCase)
        && string.Equals(instrument.QuoteAsset, "USDT", StringComparison.OrdinalIgnoreCase)
        && !ExcludedBaseAssets.Contains(instrument.BaseAsset)
        && instrument.LastStatus.Equals("Trading", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<Instrument> FilterEligible(IEnumerable<Instrument> instruments) =>
        instruments.Where(IsEligible).OrderBy(i => i.ExchangeSymbol, StringComparer.Ordinal).ToList();
}
