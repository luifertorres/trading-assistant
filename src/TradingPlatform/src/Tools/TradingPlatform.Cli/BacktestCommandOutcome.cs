using Research.Domain;

namespace TradingPlatform.Cli;

internal sealed record BacktestCommandOutcome(int ExitCode, SimulationRunResult? Result);
