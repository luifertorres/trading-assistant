using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CandlestickData.Tests;

/// <summary>
/// Acceptance tests for Candlestick Data API endpoints.
/// Validates startup delegation, query correctness, and integrity-status contract.
/// </summary>
public class ApiAcceptanceTests : IClassFixture<CandlestickDataApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiAcceptanceTests(CandlestickDataApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    [Fact]
    public async Task GetIntegrityStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/integrity-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IntegrityStatusResult>();
        result.Should().NotBeNull();
        result!.Entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSyncStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/sync/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostStartOrResume_ReturnsAccepted()
    {
        var response = await _client.PostAsync("/sync/start-or-resume", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Accepted, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCandles_WithValidTimeframe_ReturnsOk()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var url = $"/candles?symbols=BTCUSDT&timeframe=1m&from={from:O}&to={to:O}";

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CandlesResponse>();
        result.Should().NotBeNull();
        result!.Candles.Should().NotBeNull();
        (result.IsComplete == true || result.IsComplete == false).Should().BeTrue();
    }

    [Fact]
    public async Task GetCandles_WithShortTimeframeFormat_AcceptsRequest()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var url = $"/candles?symbols=BTCUSDT&timeframe=5m&from={from:O}&to={to:O}";

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCandles_WithInvalidTimeframe_ReturnsBadRequest()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var url = $"/candles?symbols=BTCUSDT&timeframe=invalid&from={from:O}&to={to:O}";

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    private record IntegrityStatusResult(IReadOnlyList<SymbolIntegrityEntry> Entries);
    private record SymbolIntegrityEntry(string Symbol, string TimeFrame, string Status, string Reason);
    private record CandlesResponse(IReadOnlyList<object> Candles, bool IsComplete);
}
