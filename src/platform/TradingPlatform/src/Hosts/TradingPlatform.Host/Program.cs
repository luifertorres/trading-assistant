using Analytics.Infrastructure;
using Execution.Infrastructure;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Infrastructure;
using Research.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });

var dataRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "TradingPlatform");
Directory.CreateDirectory(dataRoot);

var marketDb = Path.Combine(dataRoot, "market.sqlite");
var researchDb = Path.Combine(dataRoot, "research.sqlite");
var portfolioDir = Path.Combine(dataRoot, "portfolios");

builder.Services.AddMarketDataSqlite(marketDb);
builder.Services.AddResearchInfrastructure(researchDb);
builder.Services.AddAnalyticsInfrastructure();
builder.Services.AddPortfolioInfrastructure(portfolioDir);
builder.Services.AddExecutionInfrastructure();

builder.Services.AddHostedService<StartupProbeHostedService>();

var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);

/// <summary>Confirms DI wiring; replace with real workers when connecting live feeds.</summary>
internal sealed class StartupProbeHostedService(
    Execution.Infrastructure.BrokerAntiCorruptionStub broker,
    Microsoft.Extensions.Logging.ILogger<StartupProbeHostedService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await broker.EnsureConnectedAsync(stoppingToken).ConfigureAwait(false);
        log.LogInformation("TradingPlatform.Host: composition root ready (greenfield modular monolith).");
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken).ConfigureAwait(false);
    }
}
