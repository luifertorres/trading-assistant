using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Infrastructure.Events;

public sealed class WebSocketCandleEventPublisher(ILogger<WebSocketCandleEventPublisher> logger)
    : ICandleEventPublisher
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public async Task PublishCandleClosedAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime openTime,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "candle-closed",
            symbol,
            timeFrame = timeFrame.ToShortString(),
            openTime
        });

        var bytes = Encoding.UTF8.GetBytes(payload);

        foreach (var (id, ws) in _clients)
        {
            if (ws.State != WebSocketState.Open)
            {
                _clients.TryRemove(id, out _);
                continue;
            }

            try
            {
                await ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send candle-closed event to client {ClientId}", id);
                _clients.TryRemove(id, out _);
            }
        }
    }

    public void AddClient(string clientId, WebSocket webSocket)
    {
        _clients.TryAdd(clientId, webSocket);
        logger.LogInformation("WebSocket client {ClientId} connected. Total clients: {Count}", clientId, _clients.Count);
    }

    public void RemoveClient(string clientId)
    {
        _clients.TryRemove(clientId, out _);
        logger.LogInformation("WebSocket client {ClientId} disconnected. Total clients: {Count}", clientId, _clients.Count);
    }
}
