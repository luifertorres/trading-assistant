using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class CandlestickDataContextFactory : IDesignTimeDbContextFactory<CandlestickDataContext>
{
    public CandlestickDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CandlestickDataContext>();
        optionsBuilder.UseSqlite("Data Source=candlestick_data.db");

        return new CandlestickDataContext(optionsBuilder.Options);
    }
}
