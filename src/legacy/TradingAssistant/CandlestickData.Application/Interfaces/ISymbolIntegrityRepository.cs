using CandlestickData.Domain;

namespace CandlestickData.Application.Interfaces;

public interface ISymbolIntegrityRepository
{
    Task<SymbolIntegrity?> GetAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SymbolIntegrity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SymbolIntegrity integrity,
        CancellationToken cancellationToken = default);
}
