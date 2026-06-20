using CandlestickData.Application.Commands;
using MediatR;

namespace CandlestickData.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sync").WithTags("Sync");

        group.MapPost("/start-or-resume", async (ISender sender) =>
        {
            var response = await sender.Send(new StartOrResumeSyncCommand());
            return Results.Accepted(value: response);
        })
        .WithName("StartOrResumeSync");

        group.MapPost("/stop", async (ISender sender) =>
        {
            var response = await sender.Send(new StopSyncCommand());
            return Results.Ok(response);
        })
        .WithName("StopSync");

        group.MapPost("/restart", async (ISender sender) =>
        {
            var response = await sender.Send(new RestartSyncCommand());
            return Results.Accepted(value: response);
        })
        .WithName("RestartSync");
    }
}
