using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebSocketTrading.Worker;

namespace WebSocketTrading.Tests;

public sealed class TradingOptionsBindingTests
{
    [Fact]
    public void Bind_WithConfiguredVectors_DoesNotKeepCsharpDefaultOneDay()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Trading:Vectors:0:Asset"] = "DOGEUSDT",
                ["Trading:Vectors:0:Direction"] = "Short",
                ["Trading:Vectors:0:Timeframe"] = "OneMinute",
                ["Trading:Vectors:0:TradingLogic"] = "Sma200Sma5",
                ["Trading:Vectors:1:Asset"] = "DOGEUSDT",
                ["Trading:Vectors:1:Direction"] = "Long",
                ["Trading:Vectors:1:Timeframe"] = "OneMinute",
                ["Trading:Vectors:1:TradingLogic"] = "Sma200Sma5",
            })
            .Build();

        var options = new TradingOptions();
        configuration.GetSection(TradingOptions.SectionName).Bind(options);

        options.Vectors.Should().HaveCount(2);
        options.Vectors.Should().OnlyContain(v => v.Timeframe == "OneMinute");
        options.Vectors.Should().Contain(v =>
            v.Direction == WebSocketTrading.Direction.Short && v.Timeframe == "OneMinute");
        options.Vectors.Should().Contain(v =>
            v.Direction == WebSocketTrading.Direction.Long && v.Timeframe == "OneMinute");
    }

    [Fact]
    public void Bind_ProductionAppsettings_EnablesUniverse()
    {
        var appsettingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "WebSocketTrading.Worker",
            "appsettings.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath(appsettingsPath), optional: false)
            .Build();

        var options = new TradingOptions();
        configuration.GetSection(TradingOptions.SectionName).Bind(options);

        options.Universe.Enabled.Should().BeTrue();
        options.Universe.Timeframe.Should().Be("FiveMinutes");
        options.Vectors.Should().BeEmpty();
    }
}
