using CandlestickData.Application.Interfaces;
using CandlestickData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (CandlestickDataContext db) =>
        {
            try
            {
                await db.Database.CanConnectAsync();
                return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { status = "unhealthy", error = ex.Message, timestamp = DateTime.UtcNow },
                    statusCode: 503);
            }
        })
        .WithName("HealthCheck")
        .WithTags("Health");

        app.MapGet("/ready", async (
            CandlestickDataContext db,
            ISyncOrchestrator syncOrchestrator) =>
        {
            try
            {
                await db.Database.CanConnectAsync();

                return Results.Ok(new
                {
                    status = "ready",
                    syncRunning = syncOrchestrator.IsRunning,
                    timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                return Results.Json(
                    new { status = "not_ready", timestamp = DateTime.UtcNow },
                    statusCode: 503);
            }
        })
        .WithName("ReadinessCheck")
        .WithTags("Health");

        app.MapGet("/metrics", async (
            ISyncJobRepository syncJobRepository,
            ISyncCheckpointRepository checkpointRepository,
            ISymbolIntegrityRepository integrityRepository) =>
        {
            var job = await syncJobRepository.GetLatestAsync();
            var checkpoints = await checkpointRepository.GetAllAsync();
            var integrities = await integrityRepository.GetAllAsync();

            var compromisedCount = integrities.Count(i => i.Status == CandlestickData.Domain.IntegrityStatus.Compromised);
            var recoveringCount = integrities.Count(i => i.Status == CandlestickData.Domain.IntegrityStatus.Recovering);

            var freshness = checkpoints
                .Select(c => new
                {
                    c.Symbol,
                    TimeFrame = c.TimeFrame.ToString(),
                    LagSeconds = (DateTime.UtcNow - c.LastSyncedOpenTime).TotalSeconds,
                    c.UpdatedAt
                })
                .ToList();

            return Results.Ok(new
            {
                syncState = job?.State.ToString() ?? "Idle",
                symbolCount = checkpoints.Count,
                compromisedSymbols = compromisedCount,
                recoveringSymbols = recoveringCount,
                freshness,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("Metrics")
        .WithTags("Health");
    }
}
