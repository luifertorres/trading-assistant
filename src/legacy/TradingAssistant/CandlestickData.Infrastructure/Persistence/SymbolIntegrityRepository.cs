using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class SymbolIntegrityRepository(CandlestickDataContext context) : ISymbolIntegrityRepository
{
    public async Task<SymbolIntegrity?> GetAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default)
    {
        return await context.SymbolIntegrities
            .FirstOrDefaultAsync(
                i => i.Symbol == symbol && i.TimeFrame == timeFrame,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SymbolIntegrity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.SymbolIntegrities
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(SymbolIntegrity integrity, CancellationToken cancellationToken = default)
    {
        var existing = await context.SymbolIntegrities
            .FindAsync([integrity.Symbol, integrity.TimeFrame], cancellationToken);

        if (existing is null)
            context.SymbolIntegrities.Add(integrity);
        else
            context.Entry(existing).CurrentValues.SetValues(integrity);

        await context.SaveChangesAsync(cancellationToken);
    }
}
