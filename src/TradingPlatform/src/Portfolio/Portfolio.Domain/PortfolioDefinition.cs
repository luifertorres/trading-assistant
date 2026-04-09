using TradingPlatform.Kernel;

namespace Portfolio.Domain;

public sealed record PortfolioMember(TradingVectorId VectorId, decimal Weight);

/// <summary>Published language to Execution: which vectors and weights form the live portfolio.</summary>
public sealed record PortfolioDefinition(
    Guid PortfolioId,
    string Name,
    IReadOnlyList<PortfolioMember> Members,
    DateTimeOffset AsOf);
