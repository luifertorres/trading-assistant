using Binance.Net;
using CandlestickData.Api.Endpoints;
using CandlestickData.Application;
using CandlestickData.Infrastructure;
using CandlestickData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCandlestickApplication();
builder.Services.AddCandlestickInfrastructure(builder.Configuration);

builder.Services.AddBinance(options =>
{
    var apiKey = builder.Configuration["Binance:Futures:ApiKey"];
    var apiSecret = builder.Configuration["Binance:Futures:ApiSecret"];

    if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
    {
        options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(apiKey, apiSecret);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CandlestickDataContext>();
    await db.Database.MigrateAsync();
}

app.UseWebSockets();
app.MapSyncEndpoints();
app.MapCandlesEndpoints();
app.MapEventsEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program;
