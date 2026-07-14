using Binance.Net;
using WebSocketTrading.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TradingOptions>(builder.Configuration.GetSection(TradingOptions.SectionName));
builder.Services.AddBinance(options =>
{
    var apiKey = builder.Configuration["Binance:ApiKey"]
        ?? throw new InvalidOperationException("Missing Binance:ApiKey (use user secrets or environment variables).");
    var apiSecret = builder.Configuration["Binance:ApiSecret"]
        ?? throw new InvalidOperationException("Missing Binance:ApiSecret (use user secrets or environment variables).");

    options.ApiCredentials = new BinanceCredentials(apiKey, apiSecret);
});

builder.Services.AddHostedService<TradingWorker>();

var host = builder.Build();
host.Run();
