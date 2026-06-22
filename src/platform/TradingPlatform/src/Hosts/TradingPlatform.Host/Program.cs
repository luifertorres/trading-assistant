using Analytics.Infrastructure;
using Execution.Infrastructure;
using MarketData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Infrastructure;
using Research.Infrastructure;
using TradingPlatform.Host;

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
builder.Services.AddMarketDataBinanceLiveFeed();
builder.Services.AddResearchInfrastructure(researchDb);
builder.Services.AddAnalyticsInfrastructure();
builder.Services.AddPortfolioInfrastructure(portfolioDir);
builder.Services.AddExecutionInfrastructure(liveTrading: false);

builder.Services.AddHostedService<LiveStrategyWorker>();

var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);
