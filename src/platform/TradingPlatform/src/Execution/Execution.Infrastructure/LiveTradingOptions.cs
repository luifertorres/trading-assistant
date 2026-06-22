namespace Execution.Infrastructure;

/// <summary>Real-money guardrails; see .cursor/rules/live-trading-safety.mdc.</summary>
public sealed class LiveTradingOptions
{
    public bool Armed { get; set; }

    public int MaxLeverage { get; set; } = 1;

    public decimal MaxNotionalUsdPerSymbol { get; set; } = 10m;

    public decimal MaxTotalNotionalUsd { get; set; } = 40m;

    public int MaxConcurrentPositions { get; set; } = 4;

    public int MaxOrdersPerDay { get; set; } = 8;

    public string? KillSwitchFilePath { get; set; }

    public string VerdictDirectory { get; set; } = ".trading-platform-data/verdicts";

    public string PortfolioDirectory { get; set; } = ".trading-platform-data/portfolios";
}
