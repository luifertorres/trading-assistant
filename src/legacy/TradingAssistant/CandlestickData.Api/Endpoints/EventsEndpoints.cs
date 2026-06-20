using System.Net.WebSockets;
using CandlestickData.Infrastructure.Events;

namespace CandlestickData.Api.Endpoints;

public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/events", async (HttpContext context, WebSocketCandleEventPublisher publisher) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var ws = await context.WebSockets.AcceptWebSocketAsync();
            var clientId = Guid.NewGuid().ToString();

            publisher.AddClient(clientId, ws);

            try
            {
                var buffer = new byte[1024];
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(buffer, context.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                }
            }
            finally
            {
                publisher.RemoveClient(clientId);
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        });
    }
}
