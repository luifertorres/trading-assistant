using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class CandlestickDataContextFactory : IDesignTimeDbContextFactory<CandlestickDataContext>
{
    public CandlestickDataContext CreateDbContext(string[] args)
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(basePath, "TradingAssistant", "candlestick_data.db");

        var optionsBuilder = new DbContextOptionsBuilder<CandlestickDataContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new CandlestickDataContext(optionsBuilder.Options);
    }
}
