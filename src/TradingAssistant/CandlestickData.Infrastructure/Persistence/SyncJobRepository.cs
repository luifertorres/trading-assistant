using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class SyncJobRepository(CandlestickDataContext context) : ISyncJobRepository
{
    public async Task<SyncJob?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await context.SyncJobs
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        var existing = await context.SyncJobs.FindAsync([job.Id], cancellationToken);

        if (existing is null)
            context.SyncJobs.Add(job);
        else
            context.Entry(existing).CurrentValues.SetValues(job);

        await context.SaveChangesAsync(cancellationToken);
    }
}
