using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class SyncCheckpointRepository(CandlestickDataContext context) : ISyncCheckpointRepository
{
    public async Task<SyncCheckpoint?> GetAsync(
        string symbol,
        TimeFrame timeFrame,
        CancellationToken cancellationToken = default)
    {
        return await context.SyncCheckpoints
            .FirstOrDefaultAsync(
                s => s.Symbol == symbol && s.TimeFrame == timeFrame,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SyncCheckpoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.SyncCheckpoints
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var existing = await context.SyncCheckpoints
            .FindAsync([checkpoint.Symbol, checkpoint.TimeFrame], cancellationToken);

        if (existing is null)
            context.SyncCheckpoints.Add(checkpoint);
        else
            context.Entry(existing).CurrentValues.SetValues(checkpoint);

        await context.SaveChangesAsync(cancellationToken);
    }
}
