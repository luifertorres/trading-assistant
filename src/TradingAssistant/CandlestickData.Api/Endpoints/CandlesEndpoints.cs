using CandlestickData.Application.Queries;
using CandlestickData.Domain;
using MediatR;

namespace CandlestickData.Api.Endpoints;

public static class CandlesEndpoints
{
    public static void MapCandlesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/candles", async (
            string symbols,
            string timeframe,
            DateTime from,
            DateTime to,
            ISender sender) =>
        {
            var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!Enum.TryParse<TimeFrame>(timeframe, ignoreCase: true, out var tf))
                return Results.BadRequest($"Invalid timeframe: {timeframe}");

            var response = await sender.Send(new GetCandlesQuery(symbolList, tf, from, to));
            return Results.Ok(response);
        })
        .WithName("GetCandles")
        .WithTags("Candles");

        app.MapGet("/integrity-status", async (ISender sender) =>
        {
            var response = await sender.Send(new GetIntegrityStatusQuery());
            return Results.Ok(response);
        })
        .WithName("GetIntegrityStatus")
        .WithTags("Integrity");

        app.MapGet("/sync/status", async (ISender sender) =>
        {
            var response = await sender.Send(new GetSyncStatusQuery());
            return Results.Ok(response);
        })
        .WithName("GetSyncStatus")
        .WithTags("Sync");
    }
}
