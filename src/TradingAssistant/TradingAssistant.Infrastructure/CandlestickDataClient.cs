using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingAssistant.Application;

namespace TradingAssistant.Infrastructure;

public sealed class CandlestickDataClient(
    HttpClient httpClient,
    ILogger<CandlestickDataClient> logger) : ICandlestickDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<SyncCommandResult> StartOrResumeSyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync("/sync/start-or-resume", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SyncCommandResult>(cancellationToken);
            return result ?? new SyncCommandResult(Guid.Empty, "Unknown", "No response body.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to trigger sync start-or-resume on Candlestick Data API");
            return new SyncCommandResult(Guid.Empty, "Failed", ex.Message);
        }
    }

    public async Task<IntegrityStatusResult> GetIntegrityStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<IntegrityStatusResult>(
                "/integrity-status", cancellationToken);
            return result ?? new IntegrityStatusResult([]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get integrity status from Candlestick Data API");
            return new IntegrityStatusResult([]);
        }
    }

    public async Task<CandlesResult> GetCandlesAsync(
        string symbol, string timeFrame, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/candles?symbols={Uri.EscapeDataString(symbol)}&timeframe={Uri.EscapeDataString(timeFrame)}&from={from:O}&to={to:O}";
            var result = await httpClient.GetFromJsonAsync<CandlesResult>(url, cancellationToken);
            return result ?? new CandlesResult([], false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get candles from Candlestick Data API");
            return new CandlesResult([], false);
        }
    }

    public async Task SubscribeToCandleEventsAsync(
        Func<CandleClosedEvent, Task> onCandleClosed,
        CancellationToken cancellationToken = default)
    {
        var baseUri = httpClient.BaseAddress!;
        var wsUri = new UriBuilder(baseUri) { Scheme = baseUri.Scheme == "https" ? "wss" : "ws", Path = "/ws/events" }.Uri;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(wsUri, cancellationToken);
                logger.LogInformation("Connected to Candlestick Data API WebSocket at {Uri}", wsUri);

                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var evt = JsonSerializer.Deserialize<CandleClosedEvent>(json, JsonOptions);
                        if (evt is not null)
                            await onCandleClosed(evt);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WebSocket connection failed, reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
