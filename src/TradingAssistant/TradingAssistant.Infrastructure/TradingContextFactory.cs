using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradingAssistant.Infrastructure;

public class TradingContextFactory : IDesignTimeDbContextFactory<TradingContext>
{
    public TradingContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TradingContext>()
            .UseSqlite($"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/trading.db")
            .Options;

        return new TradingContext();
    }
}
